using System.Security.Claims;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Controllers;

// PIN lifecycle (#13): admins issue/reset/deactivate staff PINs, staff change their own.
// PINs stay hashed at rest. Because the PIN alone resolves the bartender at confirm time,
// a PIN must be unique among ACTIVE pins — enforced here, at set time.
[ApiController]
[Route("api/staff-pins")]
public class StaffPinsController : ControllerBase
{
    // #79: was hardcoded to exactly 6 digits; relaxed to a range so an admin can choose a
    // longer, easier-to-remember format (e.g. an 8-digit birthday, MMDDYYYY) per bartender.
    // ConfirmationsController.PostConfirmation references these same constants — same
    // cross-controller reuse pattern as MeController referencing
    // ConfirmationsController.MugGoal — so the two entry points can't drift apart.
    public const int MinPinLength = 6;
    public const int MaxPinLength = 8;

    private static readonly PasswordHasher<ApplicationUser> PinHasher = new();

    private readonly ApplicationDbContext _context;

    public StaffPinsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Bartender,Admin")]
    [HttpPut("me")]
    public async Task<IActionResult> SetMyPin(SetPinRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        return await SetPinAsync(userId, request.Pin);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{userId}")]
    public async Task<IActionResult> IssuePin(string userId, SetPinRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var staffRoleIds = await _context.Roles
            .Where(r => r.Name == "Bartender" || r.Name == "Admin")
            .Select(r => r.Id)
            .ToListAsync();
        var isStaff = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && staffRoleIds.Contains(ur.RoleId));
        if (!isStaff)
        {
            return BadRequest(new { message = "PINs can only be issued to Bartender or Admin users." });
        }

        return await SetPinAsync(userId, request.Pin);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeactivatePin(string userId)
    {
        var staffPin = await _context.StaffPins.FirstOrDefaultAsync(p => p.UserId == userId);
        if (staffPin == null)
        {
            return NotFound(new { message = "No PIN exists for that user." });
        }

        staffPin.IsActive = false;
        staffPin.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<IActionResult> SetPinAsync(string userId, string? pin)
    {
        if (pin == null || pin.Length < MinPinLength || pin.Length > MaxPinLength || !pin.All(char.IsAsciiDigit))
        {
            return BadRequest(new { message = $"A PIN of {MinPinLength}-{MaxPinLength} digits is required." });
        }

        // Hashes can't be compared directly, so the candidate is verified against each
        // other active PIN (staff counts are small). Re-using your own current PIN is fine.
        var otherActivePins = await _context.StaffPins
            .Where(p => p.IsActive && p.UserId != userId)
            .ToListAsync();
        var collides = otherActivePins.Any(p =>
            PinHasher.VerifyHashedPassword(new ApplicationUser(), p.PinHash, pin) != PasswordVerificationResult.Failed);
        if (collides)
        {
            return Conflict(new { message = "That PIN is already in use by another staff member." });
        }

        var staffPin = await _context.StaffPins.FirstOrDefaultAsync(p => p.UserId == userId);
        if (staffPin == null)
        {
            staffPin = new StaffPin { UserId = userId };
            _context.StaffPins.Add(staffPin);
        }

        // A fresh PIN starts with a clean slate: active, unlocked, counter at zero.
        staffPin.PinHash = PinHasher.HashPassword(new ApplicationUser(), pin);
        staffPin.IsActive = true;
        staffPin.FailedAttempts = 0;
        staffPin.LockedUntil = null;
        staffPin.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record SetPinRequest(string Pin);
