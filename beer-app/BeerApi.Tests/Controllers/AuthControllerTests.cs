using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BeerApi.Controllers;
using BeerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BeerApi.Tests.Controllers;

[Collection("WebApplicationFactory")]
public class AuthControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AuthControllerTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithNewEmail_CreatesCustomer_AndReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("new.customer@example.com", "Passw0rd!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body?.Token));
        Assert.Equal("new.customer@example.com", body?.Email);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        var request = new RegisterRequest("duplicate@example.com", "Passw0rd!");
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingPassword_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("no.password@example.com", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsToken()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("login.success@example.com", "Passw0rd!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("login.success@example.com", "Passw0rd!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body?.Token));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("login.wrongpass@example.com", "Passw0rd!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("login.wrongpass@example.com", "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("does.not.exist@example.com", "Passw0rd!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // #17: the paper-sheet crowd types passwords like "beer1234", not "Passw0rd!".
    // Policy is length-only (min 8) so the one rule we enforce is the one we can explain.
    [Fact]
    public async Task Register_WithCasualPasswordMeetingLength_Succeeds()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("casual.password@example.com", "beer1234"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body?.Token));
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest_WithExplanation()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("short.password@example.com", "beer123"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Contains("at least 8 characters", body?.Message);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict_WithExplanation()
    {
        var request = new RegisterRequest("duplicate.message@example.com", "Passw0rd!");
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("A user with that email already exists.", body?.Message);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsGenericMessage()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("login.message@example.com", "Passw0rd!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("login.message@example.com", "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Invalid credentials.", body?.Message);
    }

    [Fact]
    public async Task Register_WithMarketingConsentTrue_PersistsConsent()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("consent.yes@example.com", "Passw0rd!", MarketingConsent: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("consent.yes@example.com");

        Assert.True(user?.MarketingConsent);
    }

    [Fact]
    public async Task Register_WithoutMarketingConsent_DefaultsToFalse()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("consent.default@example.com", "Passw0rd!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("consent.default@example.com");

        Assert.False(user?.MarketingConsent);
    }

    [Fact]
    public async Task ForgotPassword_WithExistingEmail_ReturnsGenericSuccess_AndSendsResetEmail()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("forgot.exists@example.com", "Passw0rd!"));

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequest("forgot.exists@example.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sent = Assert.Single(_factory.EmailSender.SentEmails, e => e.ToEmail == "forgot.exists@example.com");
        Assert.Contains("reset-password?email=", sent.Body);
    }

    [Fact]
    public async Task ForgotPassword_WithUnknownEmail_ReturnsSameGenericSuccess_AndDoesNotSendEmail()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequest("no.such.account@example.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(_factory.EmailSender.SentEmails, e => e.ToEmail == "no.such.account@example.com");
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ChangesPassword_AndAllowsLoginWithNewPassword()
    {
        const string email = "reset.valid@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!"));
        await _client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(email));
        var token = ExtractTokenFromLastResetEmail(email);

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(email, token, "NewPassw0rd!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var oldPasswordLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Passw0rd!"));
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);

        var newPasswordLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "NewPassw0rd!"));
        Assert.Equal(HttpStatusCode.OK, newPasswordLogin.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest_WithGenericMessage()
    {
        const string email = "reset.invalidtoken@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!"));

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(email, "not-a-real-token", "NewPassw0rd!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("This password reset link is invalid or has expired.", body?.Message);
    }

    [Fact]
    public async Task ResetPassword_WithUnknownEmail_ReturnsSameGenericMessageAsInvalidToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest("no.such.account@example.com", "not-a-real-token", "NewPassw0rd!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("This password reset link is invalid or has expired.", body?.Message);
    }

    [Fact]
    public async Task ResetPassword_WithShortNewPassword_ReturnsBadRequest_WithExplanation()
    {
        const string email = "reset.shortpass@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!"));
        await _client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(email));
        var token = ExtractTokenFromLastResetEmail(email);

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(email, token, "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Contains("at least 8 characters", body?.Message);
    }

    private string ExtractTokenFromLastResetEmail(string email)
    {
        var sent = _factory.EmailSender.SentEmails.Last(e => e.ToEmail == email);
        var tokenParam = sent.Body.Split("token=").Last().Split('\n', ' ').First();
        return Uri.UnescapeDataString(tokenParam);
    }

    // The challenge endpoint's redirect is buildable and testable without any real
    // provider credentials or network access — it's just a 302 the OAuth handler
    // constructs locally. The callback side (after a real provider round-trip) isn't
    // covered here; see ExternalLoginServiceTests for the link-or-create logic that
    // callback ultimately delegates to.
    [Fact]
    public async Task ExternalLogin_Google_RedirectsToGoogleAuthorizeEndpoint()
    {
        using var noRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await noRedirectClient.GetAsync("/api/auth/external-login/Google");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("accounts.google.com", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task ExternalLogin_Facebook_RedirectsToFacebookAuthorizeEndpoint()
    {
        using var noRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await noRedirectClient.GetAsync("/api/auth/external-login/Facebook");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("facebook.com", response.Headers.Location?.ToString());
    }

    private static string BuildFacebookSignedRequest(string userId, string appSecret)
    {
        var payloadJson = JsonSerializer.Serialize(new { algorithm = "HMAC-SHA256", user_id = userId });
        var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), Encoding.UTF8.GetBytes(encodedPayload));
        return $"{Base64UrlEncode(signature)}.{encodedPayload}";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    [Fact]
    public async Task FacebookDataDeletion_ValidSignedRequest_AnonymizesLinkedAccount_AndReturnsConfirmation()
    {
        string userId;
        using (var setupScope = _factory.Services.CreateScope())
        {
            var userManager = setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = "fb.controller.delete@example.com", Email = "fb.controller.delete@example.com" };
            await userManager.CreateAsync(user);
            await userManager.AddLoginAsync(user, new UserLoginInfo("Facebook", "fb-controller-key", "Facebook"));
            userId = user.Id;
        }

        var signedRequest = BuildFacebookSignedRequest("fb-controller-key", TestWebApplicationFactory.FacebookAppSecret);
        var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["signed_request"] = signedRequest });

        var response = await _client.PostAsync("/api/auth/facebook/data-deletion", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("confirmation_code").GetString()));
        Assert.Contains("/privacy", body.GetProperty("url").GetString());

        using var verifyScope = _factory.Services.CreateScope();
        var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var reloaded = await verifyUserManager.FindByIdAsync(userId);
        Assert.NotEqual("fb.controller.delete@example.com", reloaded!.Email);
    }

    [Fact]
    public async Task FacebookDataDeletion_InvalidSignature_ReturnsBadRequest()
    {
        var signedRequest = BuildFacebookSignedRequest("fb-someone", "wrong-secret");
        var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["signed_request"] = signedRequest });

        var response = await _client.PostAsync("/api/auth/facebook/data-deletion", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FacebookDataDeletion_MissingSignedRequest_ReturnsBadRequest()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>());

        var response = await _client.PostAsync("/api/auth/facebook/data-deletion", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();

    private sealed record ErrorResponse(string Message);
}
