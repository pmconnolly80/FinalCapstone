namespace BeerApi.Models;

// Audit trail for admin corrections (#15): who voided what, when, and why, preserving the
// voided confirmation's original data — including the beer name at void time, so the
// record stays readable through later catalog churn. No silent deletes.
public class ConfirmationAudit
{
    public int Id { get; set; }
    public int OriginalConfirmationId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public int BeerId { get; set; }
    public string BeerName { get; set; } = string.Empty;
    public int TavernId { get; set; }
    public string ConfirmedByUserId { get; set; } = string.Empty;
    public DateTime ConfirmedAt { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
    public DateTime CorrectedAt { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;
}
