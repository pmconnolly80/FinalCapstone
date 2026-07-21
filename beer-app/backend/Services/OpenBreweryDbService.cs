using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace BeerApi.Services;

// Open Brewery DB is breweries-only, free/no-auth (TECHNICAL_ARCHITECTURE_PLAN.md §6).
// Caching is mandatory per that plan — breweries rarely move, and a lookup failure
// should never take down beer detail, so any error just yields no brewery card.
public class OpenBreweryDbService : IBreweryLookupService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public OpenBreweryDbService(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;
    }

    public async Task<BreweryInfo?> GetBreweryAsync(string breweryId)
    {
        var cacheKey = $"obdb-brewery:{breweryId}";
        if (_cache.TryGetValue(cacheKey, out BreweryInfo? cached))
        {
            return cached;
        }

        BreweryInfo? info;
        try
        {
            var response = await _httpClient.GetAsync($"breweries/{breweryId}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var dto = await response.Content.ReadFromJsonAsync<ObdbBreweryDto>(JsonOptions);
            info = dto == null ? null : new BreweryInfo(dto.Id, dto.Name, dto.BreweryType, dto.City, dto.StateProvince, dto.WebsiteUrl);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return null;
        }

        if (info != null)
        {
            _cache.Set(cacheKey, info, CacheDuration);
        }

        return info;
    }

    public async Task<IReadOnlyList<BreweryInfo>> SearchBreweriesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<BreweryInfo>();
        }

        var normalized = query.Trim().ToLowerInvariant();
        var cacheKey = $"obdb-brewery-search:{normalized}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<BreweryInfo>? cached))
        {
            return cached!;
        }

        IReadOnlyList<BreweryInfo> results;
        try
        {
            var response = await _httpClient.GetAsync($"breweries/search?query={Uri.EscapeDataString(normalized)}&per_page=8");
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<BreweryInfo>();
            }

            var dtos = await response.Content.ReadFromJsonAsync<List<ObdbBreweryDto>>(JsonOptions);
            results = (dtos ?? new List<ObdbBreweryDto>())
                .Select(dto => new BreweryInfo(dto.Id, dto.Name, dto.BreweryType, dto.City, dto.StateProvince, dto.WebsiteUrl))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return Array.Empty<BreweryInfo>();
        }

        _cache.Set(cacheKey, results, CacheDuration);
        return results;
    }

    private record ObdbBreweryDto(
        string Id,
        string Name,
        [property: JsonPropertyName("brewery_type")] string? BreweryType,
        string? City,
        [property: JsonPropertyName("state_province")] string? StateProvince,
        [property: JsonPropertyName("website_url")] string? WebsiteUrl);
}
