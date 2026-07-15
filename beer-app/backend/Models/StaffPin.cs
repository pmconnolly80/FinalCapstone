namespace BeerApi.Models;

// A bartender's personal 6-digit PIN, typed on the customer's phone to authorize and
// attribute a confirmation. Hashed like a password; never stored or returned in plaintext.
// FailedAttempts/LockedUntil are schema-ahead for the Sprint 2 lockout behavior.
public class StaffPin
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PinHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
}
