namespace BeerApi.Models;

public class Beer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brewery { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
