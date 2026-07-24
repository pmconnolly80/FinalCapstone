using System.Security.Claims;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Controllers;

// #73: a customer suggesting a beer the tavern doesn't carry. Stands alone with just a
// beer name — the "recommend from a search hit" path (#72) just pre-fills BreweryName/
// ExternalCatalogBeerId, both optional here.
[ApiController]
[Route("api/recommendations")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RecommendationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<BeerRecommendation>> Submit(SubmitRecommendationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BeerName))
        {
            return BadRequest(new { message = "A beer name is required." });
        }

        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (customerId == null)
        {
            return Unauthorized();
        }

        var recommendation = new BeerRecommendation
        {
            CustomerId = customerId,
            BeerName = request.BeerName.Trim(),
            BreweryName = string.IsNullOrWhiteSpace(request.BreweryName) ? null : request.BreweryName.Trim(),
            ExternalCatalogBeerId = string.IsNullOrWhiteSpace(request.ExternalCatalogBeerId) ? null : request.ExternalCatalogBeerId.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        };

        _context.BeerRecommendations.Add(recommendation);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Submit), new { id = recommendation.Id }, recommendation);
    }
}

public record SubmitRecommendationRequest(
    string BeerName, string? BreweryName = null, string? ExternalCatalogBeerId = null, string? Note = null);
