namespace BeerApi.Models;

// The digital replacement for the bartender's initials on the paper sheet: one row per
// beer a customer has verifiably drunk. Progress counts derive from these rows.
public class BeerConfirmation
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public int BeerId { get; set; }
    public Beer? Beer { get; set; }
    public int TavernId { get; set; }
    public Tavern? Tavern { get; set; }
    public string ConfirmedByUserId { get; set; } = string.Empty;
    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
}
