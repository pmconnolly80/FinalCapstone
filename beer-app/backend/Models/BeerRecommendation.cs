using System.Text.Json.Serialization;

namespace BeerApi.Models;

// #73: a customer suggesting a beer the tavern doesn't carry — pairs with #72's external
// search (recommend from a hit) but also stands alone as plain text.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeerRecommendationStatus
{
    New,
    Reviewed,
    Added,
    Declined,
}

public class BeerRecommendation
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string BeerName { get; set; } = string.Empty;
    public string? BreweryName { get; set; }
    public string? ExternalCatalogBeerId { get; set; }
    public string? Note { get; set; }
    public BeerRecommendationStatus Status { get; set; } = BeerRecommendationStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
