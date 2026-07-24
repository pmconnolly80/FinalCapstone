namespace BeerApi.Models;

// #74: the minimal slice of the full My Beers ratings feature (IMPLEMENTATION_BACKLOG.md
// Phase 6) — private to the rating customer, requires an existing BeerConfirmation for the
// same (CustomerId, BeerId) pair (enforced in MeController, not here), one rating per
// customer per beer, editable in place.
public class BeerRating
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public int BeerId { get; set; }
    public Beer? Beer { get; set; }
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
