using BeerApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Controllers;

// Owner/admin view of mug earners (#14): who still needs a physical mug handed over.
// API-first per grooming — the admin dashboard UI belongs to the Admin Experience epic.
[ApiController]
[Route("api/mug-awards")]
public class MugAwardsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MugAwardsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MugAwardResponse>>> GetMugAwards()
    {
        // Oldest earner first — they've been waiting for their mug the longest.
        var awards = await _context.MugAwards
            .OrderBy(a => a.EarnedAt)
            .Join(_context.Users,
                a => a.CustomerId,
                u => u.Id,
                (a, u) => new MugAwardResponse(a.CustomerId, u.Email ?? u.UserName ?? a.CustomerId, a.EarnedAt))
            .ToListAsync();

        return awards;
    }
}

public record MugAwardResponse(string CustomerId, string Email, DateTime EarnedAt);
