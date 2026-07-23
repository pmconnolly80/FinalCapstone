using System.Security.Claims;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Controllers;

// Admin user-role management (#53) — the first use of the generalized AdminAudit trail.
// Reassignment replaces the user's existing role(s) rather than adding one, matching the
// single-role-per-user model used everywhere else in the app.
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminUsersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

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
