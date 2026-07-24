namespace BeerApi.Models;

// #72: what customers search for in "look up any beer" mode (Catalog.beer/Open Brewery DB),
// logged as an ordering-decision signal for the owner. MatchedTavernCatalog lets #83's
// demand report filter to only what the tavern doesn't actually carry.
public class ExternalSearchLog
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public bool MatchedTavernCatalog { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
