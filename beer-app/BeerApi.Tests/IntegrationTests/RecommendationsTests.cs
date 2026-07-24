using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeerApi.Controllers;
using Xunit;

namespace BeerApi.Tests.IntegrationTests;

// #73: any signed-in customer can submit a recommendation; the business logic itself is
// unit-tested directly (RecommendationsControllerTests).
[Collection("WebApplicationFactory")]
public class RecommendationsTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public RecommendationsTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Submit_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/recommendations", new SubmitRecommendationRequest("Duvel"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithCustomerToken_ReturnsCreated()
    {
        var token = await RegisterCustomerAsync("recommend.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/recommendations", new SubmitRecommendationRequest("Duvel"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<string> RegisterCustomerAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!"));
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    public void Dispose() => _factory.Dispose();
}
