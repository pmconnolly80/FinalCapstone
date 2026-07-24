namespace BeerApi.Models;

// #81: a crowd-sourced signal alongside the bartender's own PIN-pad availability toggle
// (#80) — reports never change availability directly (that would be a griefing vector),
// they only surface to the admin via AdminAnomaliesController so an admin can confirm
// and flip it themselves.
public class UnavailabilityReport
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public int BeerId { get; set; }
    public Beer? Beer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
