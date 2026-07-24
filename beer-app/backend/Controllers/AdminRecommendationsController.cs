using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Controllers;

// #73: admin triage for customer beer recommendations — a write-only inbox isn't
// actionable, so this is required alongside the submission endpoint, not a follow-up.
// No reason required on status changes (unlike confirmation-void, which reverses a
// bartender-confirmed fact) — this is closer to BeersController's no-reason inline
// availability PATCH.
[ApiController]
[Route("api/admin/recommendations")]
[Authorize(Roles = "Admin")]
public class AdminRecommendationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminRecommendationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminRecommendationResponse>>> GetRecommendations(string? status)
    {
        var query = _context.BeerRecommendations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<BeerRecommendationStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                return BadRequest(new { message = $"Unknown status '{status}'." });
            }
            query = query.Where(r => r.Status == parsedStatus);
        }

        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AdminRecommendationResponse(
                r.Id,
                r.CustomerId,
                _context.Users.Where(u => u.Id == r.CustomerId).Select(u => u.Email).FirstOrDefault() ?? r.CustomerId,
                r.BeerName,
                r.BreweryName,
                r.Note,
                r.Status,
                r.CreatedAt))
            .ToListAsync();

        return rows;
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateRecommendationStatusRequest request)
    {
        var recommendation = await _context.BeerRecommendations.FindAsync(id);
        if (recommendation == null)
        {
            return NotFound();
        }

        recommendation.Status = request.Status;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record AdminRecommendationResponse(
    int Id, string CustomerId, string CustomerEmail, string BeerName, string? BreweryName,
    string? Note, BeerRecommendationStatus Status, DateTime CreatedAt);

public record UpdateRecommendationStatusRequest(BeerRecommendationStatus Status);
