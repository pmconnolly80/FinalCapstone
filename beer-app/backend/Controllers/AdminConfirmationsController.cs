using System.Security.Claims;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Controllers;

// Admin confirmation audit & correction (#15) — the first slice of admin edit-everything.
// Voiding hard-deletes the confirmation (freeing the customer+beer slot so the right beer
// can be confirmed afterwards) and writes a ConfirmationAudit row in the same save. The
// mug award is deliberately untouched: earned is permanent once stamped (see
// TECHNICAL_ARCHITECTURE_PLAN.md §4.1).
[ApiController]
[Route("api/admin/confirmations")]
[Authorize(Roles = "Admin")]
public class AdminConfirmationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminConfirmationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminConfirmationResponse>>> GetConfirmations(
        string? customerId, string? bartenderId, int? beerId, DateTime? from, DateTime? to)
    {
        var query = _context.BeerConfirmations.AsQueryable();
        if (customerId != null) query = query.Where(c => c.CustomerId == customerId);
        if (bartenderId != null) query = query.Where(c => c.ConfirmedByUserId == bartenderId);
        if (beerId != null) query = query.Where(c => c.BeerId == beerId);
        if (from != null) query = query.Where(c => c.ConfirmedAt >= from);
        if (to != null) query = query.Where(c => c.ConfirmedAt <= to);

        var rows = await query
            .OrderByDescending(c => c.ConfirmedAt)
            .Select(c => new AdminConfirmationResponse(
                c.Id,
                c.CustomerId,
                _context.Users.Where(u => u.Id == c.CustomerId).Select(u => u.Email).FirstOrDefault() ?? c.CustomerId,
                c.BeerId,
                c.Beer!.Name,
                _context.Users.Where(u => u.Id == c.ConfirmedByUserId).Select(u => u.Email).FirstOrDefault() ?? c.ConfirmedByUserId,
                c.ConfirmedAt))
            .ToListAsync();

        return rows;
    }

    [HttpGet("audits")]
    public async Task<ActionResult<IReadOnlyList<ConfirmationAuditResponse>>> GetAudits()
    {
        var audits = await _context.ConfirmationAudits
            .OrderByDescending(a => a.CorrectedAt)
            .ThenByDescending(a => a.Id)
            .Select(a => new ConfirmationAuditResponse(
                a.Id,
                _context.Users.Where(u => u.Id == a.CustomerId).Select(u => u.Email).FirstOrDefault() ?? a.CustomerId,
                a.BeerName,
                _context.Users.Where(u => u.Id == a.ConfirmedByUserId).Select(u => u.Email).FirstOrDefault() ?? a.ConfirmedByUserId,
                a.ConfirmedAt,
                _context.Users.Where(u => u.Id == a.AdminUserId).Select(u => u.Email).FirstOrDefault() ?? a.AdminUserId,
                a.CorrectedAt,
                a.Reason))
            .ToListAsync();

        return audits;
    }

    [HttpPost("{id:int}/void")]
    public async Task<IActionResult> VoidConfirmation(int id, VoidConfirmationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "A reason is required to void a confirmation." });
        }

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminId == null)
        {
            return Unauthorized();
        }

        var confirmation = await _context.BeerConfirmations
            .Include(c => c.Beer)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (confirmation == null)
        {
            return NotFound(new { message = "Confirmation not found." });
        }

        _context.ConfirmationAudits.Add(new ConfirmationAudit
        {
            OriginalConfirmationId = confirmation.Id,
            CustomerId = confirmation.CustomerId,
            BeerId = confirmation.BeerId,
            BeerName = confirmation.Beer?.Name ?? $"Beer #{confirmation.BeerId}",
            TavernId = confirmation.TavernId,
            ConfirmedByUserId = confirmation.ConfirmedByUserId,
            ConfirmedAt = confirmation.ConfirmedAt,
            AdminUserId = adminId,
            Reason = request.Reason.Trim(),
        });
        _context.BeerConfirmations.Remove(confirmation);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record AdminConfirmationResponse(
    int Id, string CustomerId, string CustomerEmail, int BeerId, string BeerName,
    string BartenderEmail, DateTime ConfirmedAt);
public record ConfirmationAuditResponse(
    int Id, string CustomerEmail, string BeerName, string BartenderEmail,
    DateTime ConfirmedAt, string AdminEmail, DateTime CorrectedAt, string Reason);
public record VoidConfirmationRequest(string Reason);
