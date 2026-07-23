using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BeerApi.Models;
using BeerApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BeerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string DefaultRole = "Customer";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEmailSender _emailSender;
    private readonly IExternalLoginService _externalLoginService;
    private readonly IAccountDeletionService _accountDeletionService;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IEmailSender emailSender,
        IExternalLoginService externalLoginService,
        IAccountDeletionService accountDeletionService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _emailSender = emailSender;
        _externalLoginService = externalLoginService;
        _accountDeletionService = accountDeletionService;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Conflict(new { message = "A user with that email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            MarketingConsent = request.MarketingConsent
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        }

        if (!await _roleManager.RoleExistsAsync(DefaultRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(DefaultRole));
        }
        await _userManager.AddToRoleAsync(user, DefaultRole);

        var token = await CreateToken(user);
        return Ok(new AuthResponse(token, user.Email!));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var token = await CreateToken(user);
        return Ok(new AuthResponse(token, user.Email!));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = $"{ResolveFrontendBaseUrl()}/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";
            await _emailSender.SendAsync(
                request.Email,
                "Reset your password",
                $"Use the link below to reset your password:\n\n{resetLink}\n\nIf you didn't request this, you can ignore this email.");
        }

        // Same response whether or not the account exists — avoids account enumeration.
        return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "Email, token, and new password are required." });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Same generic failure as an invalid/expired token — avoids account enumeration.
            return BadRequest(new { message = "This password reset link is invalid or has expired." });
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var message = result.Errors.Any(e => e.Code == "InvalidToken")
                ? "This password reset link is invalid or has expired."
                : string.Join(" ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message });
        }

        return Ok(new { message = "Your password has been reset. You can now sign in." });
    }

    // #43/#44/#45: one challenge/callback pair for every external provider — Google,
    // Facebook, and (eventually) Apple all use Identity's external-login cookie for the
    // same link-or-create-by-verified-email flow (TECHNICAL_ARCHITECTURE_PLAN.md §4.6).
    // `provider` must be the exact registered authentication scheme name (e.g. "Google").
    [AllowAnonymous]
    [HttpGet("external-login/{provider}")]
    public IActionResult ExternalLogin(string provider)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { provider });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    [AllowAnonymous]
    [HttpGet("external-login-callback")]
    public async Task<IActionResult> ExternalLoginCallback(string provider)
    {
        var frontendBaseUrl = ResolveFrontendBaseUrl();
        var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);

        if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
        {
            return Redirect($"{frontendBaseUrl}/auth?error=external_login_failed");
        }

        var principal = authenticateResult.Principal;
        var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var displayName = principal.FindFirstValue(ClaimTypes.Name);
        var emailVerified = IsEmailVerified(provider, principal);

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        if (string.IsNullOrWhiteSpace(providerKey) || string.IsNullOrWhiteSpace(email) || !emailVerified)
        {
            return Redirect($"{frontendBaseUrl}/auth?error=email_not_verified");
        }

        var result = await _externalLoginService.ProcessLoginAsync(provider, providerKey, email, displayName);
        var token = await CreateToken(result.User);

        return Redirect($"{frontendBaseUrl}/auth/callback?token={Uri.EscapeDataString(token)}");
    }

    // Facebook's required data-deletion callback (#44): a user removing this app from
    // their Facebook account triggers a signed POST here. https://developers.facebook.com/
    // docs/facebook-login/features-reference/deletion-callback
    [AllowAnonymous]
    [HttpPost("facebook/data-deletion")]
    public async Task<IActionResult> FacebookDataDeletion([FromForm(Name = "signed_request")] string? signedRequest)
    {
        var appSecret = _configuration["Authentication:Facebook:AppSecret"];
        if (string.IsNullOrWhiteSpace(signedRequest) || string.IsNullOrWhiteSpace(appSecret) ||
            !FacebookSignedRequestParser.TryParse(signedRequest, appSecret, out var facebookUserId) ||
            facebookUserId == null)
        {
            return BadRequest(new { message = "Invalid signed request." });
        }

        var confirmationCode = await _accountDeletionService.AnonymizeAsync("Facebook", facebookUserId);
        var statusUrl = $"{ResolveFrontendBaseUrl()}/privacy?deletion={Uri.EscapeDataString(confirmationCode)}";

        return Ok(new { url = statusUrl, confirmation_code = confirmationCode });
    }

    // Each provider proves "verified" differently: Google's userinfo response carries an
    // explicit verified_email flag (mapped to the email_verified claim in Program.cs);
    // Facebook's Graph API only ever returns addresses it has itself verified, so the
    // claim's mere presence is enough — there's no separate flag to check.
    private static bool IsEmailVerified(string provider, ClaimsPrincipal principal) => provider switch
    {
        "Google" => principal.FindFirstValue("email_verified") == "true",
        "Facebook" => !string.IsNullOrWhiteSpace(principal.FindFirstValue(ClaimTypes.Email)),
        _ => false,
    };

    // A configured Frontend:BaseUrl always wins — Origin can't be trusted to build a link
    // that survives being copy-pasted or opened later (a different device, an email client
    // that strips it), whereas the fallback (Origin, then localhost) only matters for local
    // dev boxes without the setting.
    private string ResolveFrontendBaseUrl()
    {
        var configured = _configuration["Frontend:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        var origin = Request.Headers.Origin.FirstOrDefault();
        return (!string.IsNullOrWhiteSpace(origin) ? origin : "http://localhost:3001").TrimEnd('/');
    }

    private async Task<string> CreateToken(ApplicationUser user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "development-secret-key-change-me";
        var issuer = _configuration["Jwt:Issuer"] ?? "beer-api";
        var audience = _configuration["Jwt:Audience"] ?? "beer-client";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record RegisterRequest(string Email, string Password, bool MarketingConsent = false);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Email);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
