using System.Security.Claims;
using BeerApi.Data;
using BeerApi.Models;
using BeerApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Controllers;

// Admin user-role management (#53) — the first use of the generalized AdminAudit trail.
// Reassignment replaces the user's existing role(s) rather than adding one, matching the
// single-role-per-user model used everywhere else in the app.
//
// #54 adds the users list (role, active/locked status, staff-PIN presence — reusing
// StaffPin rather than duplicating it) and reversible account deactivate/reactivate.
// Deactivation piggybacks on ASP.NET Identity's own lockout mechanism (LockoutEnd/
// LockoutEnabled) instead of a bespoke flag, enforced the moment AuthController.Login
// checks IsLockedOutAsync. Per TECHNICAL_ARCHITECTURE_PLAN.md §4.1, deactivating a staff
// account also deactivates their PIN everywhere, instantly — reactivating an account does
// NOT restore the PIN, since silently re-enabling a possibly-compromised PIN isn't safe
// to do implicitly.
//
// #77: admin-initiated bartender invite — creates the ApplicationUser (Bartender role)
// directly instead of requiring the new hire to self-register as a Customer first, then
// reuses the existing forgot/reset-password token flow (AuthController) as the "set your
// password" link, since ResetPasswordAsync works the same whether or not a password was
// ever set. No separate "set password" page needed — ResetPassword.jsx already does this.
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public AdminUsersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IEmailSender emailSender,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    [HttpPost("invite-bartender")]
    public async Task<IActionResult> InviteBartender(InviteBartenderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Conflict(new { message = "A user with that email already exists." });
        }

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminId == null)
        {
            return Unauthorized();
        }

        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            return BadRequest(new { message = string.Join(" ", createResult.Errors.Select(e => e.Description)) });
        }

        if (!await _roleManager.RoleExistsAsync("Bartender"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Bartender"));
        }
        await _userManager.AddToRoleAsync(user, "Bartender");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var setPasswordLink = $"{ResolveFrontendBaseUrl()}/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";
        await _emailSender.SendAsync(
            request.Email,
            "You've been invited to join The Tavern's staff",
            $"An admin has set up a bartender account for you. Use the link below to set your password:\n\n{setPasswordLink}");

        _context.AdminAudits.Add(new AdminAudit
        {
            AdminUserId = adminId,
            EntityType = "User",
            EntityId = user.Id,
            Action = "Invite",
            BeforeSnapshot = null,
            AfterSnapshot = $"Bartender ({user.Email})",
            Reason = string.Empty,
        });
        await _context.SaveChangesAsync();

        return Ok(new AdminUserResponse(user.Id, user.Email!, "Bartender", true, false));
    }

    // A configured Frontend:BaseUrl always wins for the same reason AuthController's
    // ResolveFrontendBaseUrl prefers it — an invite link needs to survive being opened
    // later, possibly on a different device, not just whatever Origin sent this request.
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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> GetUsers()
    {
        var users = await _context.Users.ToListAsync();

        // GroupBy rather than ToDictionaryAsync: the app's own AssignRole always leaves a
        // user with exactly one role, but this shouldn't 500 the whole list if a user ever
        // ends up with more than one (e.g. a manual DB correction) — just show one of them.
        var roleByUserId = (await (
            from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            select new { ur.UserId, r.Name }
        ).ToListAsync())
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.First().Name ?? "(none)");

        var activePinUserIds = (await _context.StaffPins
            .Where(p => p.IsActive)
            .Select(p => p.UserId)
            .ToListAsync())
            .ToHashSet();

        var now = DateTimeOffset.UtcNow;
        var rows = users
            .Select(u => new AdminUserResponse(
                u.Id,
                u.Email ?? string.Empty,
                roleByUserId.GetValueOrDefault(u.Id, "(none)"),
                !(u.LockoutEnd.HasValue && u.LockoutEnd > now),
                activePinUserIds.Contains(u.Id)))
            .ToList();

        return rows;
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateAccount(string id, AccountActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "A reason is required to deactivate an account." });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminId == null)
        {
            return Unauthorized();
        }

        var wasActive = IsActive(user);

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        // Deactivating a staff account deactivates the PIN everywhere, instantly
        // (TECHNICAL_ARCHITECTURE_PLAN.md §4.1) — mirrors StaffPinsController.DeactivatePin.
        var pin = await _context.StaffPins.FirstOrDefaultAsync(p => p.UserId == id);
        if (pin != null && pin.IsActive)
        {
            pin.IsActive = false;
            pin.UpdatedAt = DateTime.UtcNow;
        }

        _context.AdminAudits.Add(new AdminAudit
        {
            AdminUserId = adminId,
            EntityType = "User",
            EntityId = id,
            Action = "Deactivate",
            BeforeSnapshot = wasActive ? "Active" : "Deactivated",
            AfterSnapshot = "Deactivated",
            Reason = request.Reason.Trim(),
        });
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/reactivate")]
    public async Task<IActionResult> ReactivateAccount(string id, AccountActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "A reason is required to reactivate an account." });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminId == null)
        {
            return Unauthorized();
        }

        var wasActive = IsActive(user);

        await _userManager.SetLockoutEndDateAsync(user, null);

        _context.AdminAudits.Add(new AdminAudit
        {
            AdminUserId = adminId,
            EntityType = "User",
            EntityId = id,
            Action = "Reactivate",
            BeforeSnapshot = wasActive ? "Active" : "Deactivated",
            AfterSnapshot = "Active",
            Reason = request.Reason.Trim(),
        });
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static bool IsActive(ApplicationUser user) =>
        !(user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow);

    [HttpPut("{id}/role")]
    public async Task<IActionResult> AssignRole(string id, AssignRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "A reason is required to change a user's role." });
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            return BadRequest(new { message = "Invalid role." });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminId == null)
        {
            return Unauthorized();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var previousRole = currentRoles.FirstOrDefault() ?? "(none)";

        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }
        var addResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!addResult.Succeeded)
        {
            return BadRequest(new { message = string.Join(" ", addResult.Errors.Select(e => e.Description)) });
        }

        _context.AdminAudits.Add(new AdminAudit
        {
            AdminUserId = adminId,
            EntityType = "User",
            EntityId = id,
            Action = "RoleChange",
            BeforeSnapshot = previousRole,
            AfterSnapshot = request.Role,
            Reason = request.Reason.Trim(),
        });
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record AssignRoleRequest(string Role, string Reason);
public record AccountActionRequest(string Reason);
public record AdminUserResponse(string Id, string Email, string Role, bool IsActive, bool HasActivePin);
public record InviteBartenderRequest(string Email);
