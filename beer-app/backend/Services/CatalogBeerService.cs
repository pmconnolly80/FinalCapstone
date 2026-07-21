using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace BeerApi.Services;

// Catalog.beer beer-level pre-fill (#31, go decision — see TECHNICAL_ARCHITECTURE_PLAN.md
// §6 for the hit-rate spike). Free account, Basic auth (API key as username), default
// 1,000 requests/month — this is admin add/edit-time only, never the customer's request
// path, and cached like the OBDB lookups so repeated searches don't burn the budget.
public class CatalogBeerService : ICatalogBeerService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly string? _apiKey;

    public CatalogBeerService(HttpClient httpClient, IMemoryCache cache, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _cache = cache;
        _apiKey = configuration["CatalogBeer:ApiKey"];
    }

    public async Task<IReadOnlyList<CatalogBeerResult>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(_apiKey))
        {
            return Array.Empty<CatalogBeerResult>();
        }

        var normalized = query.Trim().ToLowerInvariant();
        var cacheKey = $"catalogbeer-search:{normalized}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<CatalogBeerResult>? cached))
        {
            return cached!;
        }

        IReadOnlyList<CatalogBeerResult> results;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"beer/search?q={Uri.EscapeDataString(normalized)}&count=10")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_apiKey}:"))) },
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<CatalogBeerResult>();
            }

            var dto = await response.Content.ReadFromJsonAsync<SearchResponseDto>(JsonOptions);
            results = (dto?.Data ?? new List<CatalogBeerDto>())
                .OrderByDescending(b => b.CbVerified)
                .Select(b => new CatalogBeerResult(
                    b.Id, b.Name, b.Style, b.Parent,
                    b.Class == null ? null : char.ToUpperInvariant(b.Class[0]) + b.Class[1..],
                    b.Description, b.Abv, b.Ibu, b.CbVerified, b.Brewer?.Name))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return Array.Empty<CatalogBeerResult>();
        }

        _cache.Set(cacheKey, results, CacheDuration);
        return results;
    }

    private record SearchResponseDto(List<CatalogBeerDto>? Data);

    private record CatalogBeerDto(
        string Id,
        string Name,
        string? Style,
        string? Parent,
        string? Class,
        string? Description,
        double? Abv,
        int? Ibu,
        [property: JsonPropertyName("cb_verified")] bool CbVerified,
        BrewerDto? Brewer);

    private record BrewerDto(string? Name);
}
