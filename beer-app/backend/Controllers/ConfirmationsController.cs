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

    private static readonly PasswordHasher<IdentityUser> PinHasher = new();

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

        if (request.Pin == null || request.Pin.Length != 6 || !request.Pin.All(char.IsAsciiDigit))
        {
            return BadRequest(new { message = "A 6-digit PIN is required." });
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

        var bartenderId = await ResolveBartenderFromPinAsync(request.Pin);
        if (bartenderId == null)
        {
            // Deliberately generic: the PIN is typed on an untrusted device.
            return Unauthorized(new { message = "Invalid PIN." });
        }

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

        return StatusCode(StatusCodes.Status201Created, new ConfirmationResponse(
            beer.Id,
            beer.Name,
            confirmation.ConfirmedAt,
            confirmedCount,
            MugGoal,
            confirmedCount >= MugGoal));
    }

    // Staff PINs are hashed, so identity is resolved by verifying the candidate PIN against
    // each active staff pin (staff counts are small). Only Bartender/Admin-role holders count.
    private async Task<string?> ResolveBartenderFromPinAsync(string pin)
    {
        var staffRoleIds = await _context.Roles
            .Where(r => r.Name == "Bartender" || r.Name == "Admin")
            .Select(r => r.Id)
            .ToListAsync();

        var activePins = await _context.StaffPins
            .Where(p => p.IsActive)
            .Where(p => _context.UserRoles.Any(ur => ur.UserId == p.UserId && staffRoleIds.Contains(ur.RoleId)))
            .ToListAsync();

        foreach (var staffPin in activePins)
        {
            var result = PinHasher.VerifyHashedPassword(new IdentityUser(), staffPin.PinHash, pin);
            if (result != PasswordVerificationResult.Failed)
            {
                return staffPin.UserId;
            }
        }

        return null;
    }
}

public record ConfirmationRequest(int BeerId, string Pin);
public record ConfirmationResponse(int BeerId, string BeerName, DateTime ConfirmedAt, int ConfirmedCount, int Goal, bool MugEarned);
