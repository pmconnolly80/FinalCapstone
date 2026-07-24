using System.Security.Claims;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Controllers;

[ApiController]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet("progress")]
    public async Task<ActionResult<ProgressResponse>> GetProgress()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (customerId == null)
        {
            return Unauthorized();
        }

        var confirmations = await _context.BeerConfirmations
            .Where(c => c.CustomerId == customerId)
            .Include(c => c.Beer)
            .OrderByDescending(c => c.ConfirmedAt)
            .Select(c => new ConfirmedBeer(
                c.BeerId,
                c.Beer!.Name,
                c.Beer.Brewery,
                c.Beer.Style,
                c.ConfirmedAt))
            .ToListAsync();

        // Earned status is the stored award (#14), not the live count — it survives
        // catalog churn and confirmation corrections.
        var award = await _context.MugAwards.FirstOrDefaultAsync(a => a.CustomerId == customerId);

        return new ProgressResponse(
            confirmations.Count,
            ConfirmationsController.MugGoal,
            award != null,
            award?.EarnedAt,
            confirmations);
    }

    // #74: the minimal slice of My Beers ratings — upserts in place (SetMyPin-style
    // "PUT to set or change" rather than separate create/update endpoints), requires an
    // existing confirmation for the same beer so a customer can't rate one they haven't
    // actually had.
    [Authorize]
    [HttpPut("ratings/{beerId}")]
    public async Task<IActionResult> SetRating(int beerId, SetRatingRequest request)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (customerId == null)
        {
            return Unauthorized();
        }

        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest(new { message = "Rating must be between 1 and 5." });
        }

        var hasConfirmation = await _context.BeerConfirmations
            .AnyAsync(c => c.CustomerId == customerId && c.BeerId == beerId);
        if (!hasConfirmation)
        {
            return BadRequest(new { message = "You can only rate a beer you've confirmed." });
        }

        var rating = await _context.BeerRatings
            .FirstOrDefaultAsync(r => r.CustomerId == customerId && r.BeerId == beerId);
        if (rating == null)
        {
            rating = new BeerRating { CustomerId = customerId, BeerId = beerId, Rating = request.Rating };
            _context.BeerRatings.Add(rating);
        }
        else
        {
            rating.Rating = request.Rating;
            rating.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record ConfirmedBeer(int BeerId, string Name, string Brewery, string Style, DateTime ConfirmedAt);
public record ProgressResponse(int ConfirmedCount, int Goal, bool MugEarned, DateTime? MugEarnedAt, IReadOnlyList<ConfirmedBeer> Confirmations);
public record SetRatingRequest(int Rating);
