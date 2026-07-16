namespace BeerApi.Models;

// Server-side record of every rejected confirmation attempt (#12). Powers the
// per-customer rolling-window lockout and preserves the real rejection reason that the
// deliberately generic 401 "Invalid PIN." hides from the (untrusted) device. A customer's
// rows are cleared by their next successful confirmation.
public class FailedConfirmationAttempt
{
    public const string ReasonWrongPin = "wrong-pin";
    public const string ReasonPinLocked = "pin-locked";
    public const string ReasonCustomerBlocked = "customer-blocked";

    public int Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;
}
