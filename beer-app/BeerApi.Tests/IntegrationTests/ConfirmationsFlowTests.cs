using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using Xunit;

namespace BeerApi.Tests.IntegrationTests;

// End-to-end coverage of the one-device confirmation loop: a customer's session plus the
// seeded dev bartender's PIN, through the real HTTP pipeline (auth middleware included).
[Collection("WebApplicationFactory")]
public class ConfirmationsFlowTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public ConfirmationsFlowTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task PostConfirmation_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/confirmations", new ConfirmationRequest(1, SeedData.DevBartenderPin));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProgress_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/me/progress");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmationLoop_CustomerSessionPlusBartenderPin_UpdatesProgress()
    {
        var token = await RegisterCustomerAsync("mugchaser@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var beers = (await _client.GetFromJsonAsync<BeerSearchResponse>("/api/beers"))!.Items;
        var beer = beers![0];

        // Wrong PIN is rejected and records nothing.
        var wrongPin = await _client.PostAsJsonAsync("/api/confirmations", new ConfirmationRequest(beer.Id, "000000"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPin.StatusCode);

        // The bartender keys the right PIN on the customer's phone.
        var confirmed = await _client.PostAsJsonAsync("/api/confirmations", new ConfirmationRequest(beer.Id, SeedData.DevBartenderPin));
        Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
        var confirmation = await confirmed.Content.ReadFromJsonAsync<ConfirmationResponse>();
        Assert.Equal(1, confirmation!.ConfirmedCount);
        Assert.Equal(beer.Name, confirmation.BeerName);

        // A beer counts once, ever.
        var duplicate = await _client.PostAsJsonAsync("/api/confirmations", new ConfirmationRequest(beer.Id, SeedData.DevBartenderPin));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        // The customer's progress reflects it.
        var progress = await _client.GetFromJsonAsync<ProgressResponse>("/api/me/progress");
        Assert.Equal(1, progress!.ConfirmedCount);
        Assert.False(progress.MugEarned);
        Assert.Equal(beer.Name, Assert.Single(progress.Confirmations).Name);
    }

    // #12: through the real pipeline, a locked-out correct PIN must be indistinguishable
    // from a plain wrong guess — same status, byte-identical body.
    [Fact]
    public async Task Lockout_AfterRepeatedWrongPins_CorrectPinRejectedWithGenericBody()
    {
        var token = await RegisterCustomerAsync("bruteforcer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var beers = (await _client.GetFromJsonAsync<BeerSearchResponse>("/api/beers"))!.Items;
        var beer = beers![0];

        string wrongBody = string.Empty;
        for (var i = 0; i < ConfirmationsController.MaxPinFailures; i++)
        {
            var wrong = await _client.PostAsJsonAsync("/api/confirmations", new ConfirmationRequest(beer.Id, "000000"));
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            wrongBody = await wrong.Content.ReadAsStringAsync();
        }

        var locked = await _client.PostAsJsonAsync("/api/confirmations", new ConfirmationRequest(beer.Id, SeedData.DevBartenderPin));

        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Equal(wrongBody, await locked.Content.ReadAsStringAsync());
    }

    private async Task<string> RegisterCustomerAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!"));
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    public void Dispose() => _factory.Dispose();
}
