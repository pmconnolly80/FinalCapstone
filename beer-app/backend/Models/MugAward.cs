namespace BeerApi.Models;

// The durable mug-earned milestone (#14), stamped once when the 200th confirmation lands.
// Deliberately not derived from the live confirmation count: earned status must survive
// later catalog churn and admin corrections. The physical mug handover reads this table.
public class MugAward
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public int TavernId { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}
