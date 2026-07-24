using System.Security.Claims;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Controllers;

// One-device confirmation (TECHNICAL_ARCHITECTURE_PLAN.md §4.1): the request is
// authenticated as the CUSTOMER whose list it is; the bartender is identified and
// authorized by the personal PIN they type on the customer's phone. The customer id
// comes from the session, never the request body.
[ApiController]
[Route("api/[controller]")]
public class ConfirmationsController : ControllerBase
{
    public const int MugGoal = 200;

    // Lockout policy (#12) — defaults from grooming, tune later. Both axes stay at the
    // same threshold so neither becomes the weaker path for a brute-forcer.
    public const int MaxPinFailures = 5;
    public const int PinLockoutMinutes = 15;
    public const int MaxCustomerFailures = 5;
    public const int CustomerWindowMinutes = 15;

    private static readonly PasswordHasher<ApplicationUser> PinHasher = new();

    private readonly ApplicationDbContext _context;

    public ConfirmationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> PostConfirmation(ConfirmationRequest request)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (customerId == null)
        {
            return Unauthorized();
        }

        if (request.Pin == null
            || request.Pin.Length < StaffPinsController.MinPinLength
            || request.Pin.Length > StaffPinsController.MaxPinLength
            || !request.Pin.All(char.IsAsciiDigit))
        {
            return BadRequest(new
            {
                message = $"A PIN of {StaffPinsController.MinPinLength}-{StaffPinsController.MaxPinLength} digits is required."
            });
        }

        var beer = await _context.Beers.FindAsync(request.BeerId);
        if (beer == null)
        {
            return NotFound(new { message = "Beer not found." });
        }

        var alreadyConfirmed = await _context.BeerConfirmations
            .AnyAsync(c => c.CustomerId == customerId && c.BeerId == request.BeerId);
        if (alreadyConfirmed)
        {
            return Conflict(new { message = "This beer is already on your confirmed list." });
        }

        var now = DateTime.UtcNow;

        // Per-customer axis (#12): a rolling window of recent failures blocks the account
        // itself, so spreading guesses across different staff PINs doesn't help. Checked
        // before PIN verification so a blocked customer learns nothing — not even whether
        // their guess was right.
        var windowStart = now.AddMinutes(-CustomerWindowMinutes);
        var recentFailures = await _context.FailedConfirmationAttempts
            .CountAsync(a => a.CustomerId == customerId && a.AttemptedAt >= windowStart);
        if (recentFailures >= MaxCustomerFailures)
        {
            return await RejectAsync(customerId, FailedConfirmationAttempt.ReasonCustomerBlocked, now);
        }

        var (bartenderId, matchedLockedPin) = await ResolveBartenderFromPinAsync(request.Pin, now);
        if (matchedLockedPin)
        {
            // The correct PIN while its lock is active is rejected like any wrong guess.
            return await RejectAsync(customerId, FailedConfirmationAttempt.ReasonPinLocked, now);
        }
        if (bartenderId == null)
        {
            return await RejectAsync(customerId, FailedConfirmationAttempt.ReasonWrongPin, now);
        }

        // Success clears the customer's failure window (both axes are about consecutive
        // failures, not lifetime totals).
        var customerFailures = await _context.FailedConfirmationAttempts
            .Where(a => a.CustomerId == customerId)
            .ToListAsync();
        _context.FailedConfirmationAttempts.RemoveRange(customerFailures);

        var tavern = await _context.Taverns.OrderBy(t => t.Id).FirstOrDefaultAsync();
        if (tavern == null)
        {
            return Problem("No tavern is configured.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var confirmation = new BeerConfirmation
        {
            CustomerId = customerId,
            BeerId = beer.Id,
            TavernId = tavern.Id,
            ConfirmedByUserId = bartenderId,
        };
        _context.BeerConfirmations.Add(confirmation);
        await _context.SaveChangesAsync();

        var confirmedCount = await _context.BeerConfirmations.CountAsync(c => c.CustomerId == customerId);

        // Durable milestone (#14): stamp the award exactly once, at the moment the goal
        // is reached. Earned status is read from this record from then on, never
        // recomputed from the live count.
        var award = await _context.MugAwards.FirstOrDefaultAsync(a => a.CustomerId == customerId);
        if (award == null && confirmedCount >= MugGoal)
        {
            award = new MugAward { CustomerId = customerId, TavernId = tavern.Id };
            _context.MugAwards.Add(award);
            await _context.SaveChangesAsync();
        }

        return StatusCode(StatusCodes.Status201Created, new ConfirmationResponse(
            beer.Id,
            beer.Name,
            confirmation.ConfirmedAt,
            confirmedCount,
            MugGoal,
            award != null));
    }

    // Persists any staged StaffPin counter changes alongside the failure record, then
    // returns the one generic rejection every axis shares — the FailedConfirmationAttempt
    // row is where the real reason lives, server-side only.
    private async Task<IActionResult> RejectAsync(string customerId, string reason, DateTime now)
    {
        _context.FailedConfirmationAttempts.Add(new FailedConfirmationAttempt
        {
            CustomerId = customerId,
            AttemptedAt = now,
            Reason = reason,
        });
        await _context.SaveChangesAsync();

        return Unauthorized(new { message = "Invalid PIN." });
    }

    // Staff PINs are hashed, so identity is resolved by verifying the candidate PIN against
    // each active staff pin (staff counts are small). Only Bartender/Admin-role holders count.
    // Counter changes are staged on the tracked entities; the caller's SaveChanges persists
    // them on both the success and failure paths.
    private async Task<(string? BartenderId, bool MatchedLockedPin)> ResolveBartenderFromPinAsync(string pin, DateTime now)
    {
        var staffRoleIds = await _context.Roles
            .Where(r => r.Name == "Bartender" || r.Name == "Admin")
            .Select(r => r.Id)
            .ToListAsync();

        var activePins = await _context.StaffPins
            .Where(p => p.IsActive)
            .Where(p => _context.UserRoles.Any(ur => ur.UserId == p.UserId && staffRoleIds.Contains(ur.RoleId)))
            .ToListAsync();

        // Expired locks reset lazily, so each lockout period starts a fresh count.
        foreach (var expired in activePins.Where(p => p.LockedUntil != null && p.LockedUntil <= now))
        {
            expired.FailedAttempts = 0;
            expired.LockedUntil = null;
        }

        foreach (var staffPin in activePins)
        {
            var result = PinHasher.VerifyHashedPassword(new ApplicationUser(), staffPin.PinHash, pin);
            if (result != PasswordVerificationResult.Failed)
            {
                if (staffPin.LockedUntil != null && staffPin.LockedUntil > now)
                {
                    return (null, true);
                }
                staffPin.FailedAttempts = 0;
                staffPin.LockedUntil = null;
                return (staffPin.UserId, false);
            }
        }

        // No match: every unlocked PIN survived a guess — count it against each of them.
        // (A candidate PIN is equidistant from every real one; the per-customer axis is
        // what stops one hostile account from freezing the whole bar.)
        foreach (var staffPin in activePins.Where(p => p.LockedUntil == null))
        {
            staffPin.FailedAttempts++;
            if (staffPin.FailedAttempts >= MaxPinFailures)
            {
                staffPin.LockedUntil = now.AddMinutes(PinLockoutMinutes);
            }
        }

        return (null, false);
    }
}

public record ConfirmationRequest(int BeerId, string Pin);
public record ConfirmationResponse(int BeerId, string BeerName, DateTime ConfirmedAt, int ConfirmedCount, int Goal, bool MugEarned);
