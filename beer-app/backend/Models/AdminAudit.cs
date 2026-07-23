namespace BeerApi.Models;

// Generalized admin-correction audit trail (#53): who changed what entity, when, and why.
// Mirrors ConfirmationAudit's shape but isn't tied to confirmations specifically — the
// first use is role reassignment, with beer edits (#56) expected to follow the same table
// rather than getting their own. Confirmations keep their own existing ConfirmationAudit
// trail; this is additive, not a replacement.
public class AdminAudit
{
    public int Id { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? BeforeSnapshot { get; set; }
    public string? AfterSnapshot { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
