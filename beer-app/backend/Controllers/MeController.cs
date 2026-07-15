using System.Security.Claims;
using BeerApi.Data;
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

        return new ProgressResponse(
            confirmations.Count,
            ConfirmationsController.MugGoal,
            confirmations.Count >= ConfirmationsController.MugGoal,
            confirmations);
    }
}

public record ConfirmedBeer(int BeerId, string Name, string Brewery, string Style, DateTime ConfirmedAt);
public record ProgressResponse(int ConfirmedCount, int Goal, bool MugEarned, IReadOnlyList<ConfirmedBeer> Confirmations);
