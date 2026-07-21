using System.Text.Json.Serialization;

namespace BeerApi.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeerAvailability
{
    OnTap,
    Available,
    OutOfStock,
    Retired,
}

public class Beer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brewery { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string? Description { get; set; }
    public BeerAvailability Availability { get; set; } = BeerAvailability.Available;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
