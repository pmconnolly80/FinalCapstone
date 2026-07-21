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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeerClass
{
    Ale,
    Lager,
}

public class Beer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brewery { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string? Description { get; set; }
    public BeerAvailability Availability { get; set; } = BeerAvailability.Available;

    // Beer-nerd stats (#29) — auto-sourced later by the Catalog.beer spike (#31), always
    // admin-editable since the tavern's list stays the source of truth.
    public double? Abv { get; set; }
    public int? Ibu { get; set; }
    public string? StyleFamily { get; set; }
    public BeerClass? Class { get; set; }

    // Open Brewery DB is breweries-only (TECHNICAL_ARCHITECTURE_PLAN.md §6) — this just
    // links to their record; the resolved brewery card is fetched (and cached) live.
    public string? ObdbBreweryId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
