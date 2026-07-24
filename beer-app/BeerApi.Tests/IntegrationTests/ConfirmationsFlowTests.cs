using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
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

    // #79: regression coverage for the full confirm + lockout flow against a bartender
    // issued a longer, non-default PIN (e.g. an 8-digit birthday format) — not just the
    // seeded dev bartender's 6-digit one, which every other test in this file already
    // exercises. Seeds directly via the DbContext rather than through the admin API,
    // since PIN-issuance itself is StaffPinLifecycleTests' concern, not this file's.
    [Fact]
    public async Task ConfirmationLoop_WithEightDigitBartenderPin_UpdatesProgress_AndLockoutStillApplies()
    {
        const string eightDigitPin = "07041999";
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var bartender = new ApplicationUser { UserName = "birthday-pin-bartender@example.com", Email = "birthday-pin-bartender@example.com" };
            await userManager.CreateAsync(bartender, "Bartender1!");
            await userManager.AddToRoleAsync(bartender, "Bartender");

            var hasher = new PasswordHasher<ApplicationUser>();
            context.StaffPins.Add(new StaffPin
            {
                UserId = bartender.Id,
                PinHash = hasher.HashPassword(bartender, eightDigitPin),
                IsActive = true,
            });
            await context.SaveChangesAsync();
        }

        var token = await RegisterCustomerAsync("birthday-pin-customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var beers = (await _client.GetFromJsonAsync<BeerSearchResponse>("/api/beers"))!.Items;

        // "A beer counts once, ever" means the already-confirmed check runs before PIN
        // verification — so the success case and the lockout case need distinct beers,
        // or every lockout-loop attempt would 409 on the first beer regardless of PIN.
        var confirmed = await _client.PostAsJsonAsync("/api/confirmations", new ConfirmationRequest(beers![0].Id, eightDigitPin));
        Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);

        // Lockout still applies the same way against an 8-digit PIN as a 6-digit one.
        var lockoutBeer = beers[1];
        string wrongBody = string.Empty;
        for (var i = 0; i < ConfirmationsController.MaxPinFailures; i++)
        {
            var wrong = await _client.PostAsJsonAsync("/api/confirmations", new ConfirmationRequest(lockoutBeer.Id, "00000000"));
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            wrongBody = await wrong.Content.ReadAsStringAsync();
        }

        var locked = await _client.PostAsJsonAsync("/api/confirmations", new ConfirmationRequest(lockoutBeer.Id, eightDigitPin));
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Equal(wrongBody, await locked.Content.ReadAsStringAsync());
    }

    // #80: a bartender flips a beer's availability using the same PIN they'd type to
    // confirm one — no separate Admin session or role gate. HTTP-level happy path in
    // both directions, plus the wrong/locked PIN rejecting it the same generic way a
    // bad confirmation PIN is rejected.
    [Fact]
    public async Task AvailabilityFlip_WithValidPin_TogglesBothDirections_AttributedToBartender()
    {
        var token = await RegisterCustomerAsync("availability-flip-customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var beers = (await _client.GetFromJsonAsync<BeerSearchResponse>("/api/beers"))!.Items;
        var beer = beers![0];

        var markOutOfStock = await _client.PostAsJsonAsync("/api/confirmations/availability",
            new PinAvailabilityRequest(beer.Id, SeedData.DevBartenderPin, BeerAvailability.OutOfStock));
        Assert.Equal(HttpStatusCode.NoContent, markOutOfStock.StatusCode);

        var afterOutOfStock = await _client.GetFromJsonAsync<BeerDetailResponse>($"/api/beers/{beer.Id}");
        Assert.Equal(BeerAvailability.OutOfStock, afterOutOfStock!.Availability);

        var markAvailable = await _client.PostAsJsonAsync("/api/confirmations/availability",
            new PinAvailabilityRequest(beer.Id, SeedData.DevBartenderPin, BeerAvailability.Available));
        Assert.Equal(HttpStatusCode.NoContent, markAvailable.StatusCode);

        var afterAvailable = await _client.GetFromJsonAsync<BeerDetailResponse>($"/api/beers/{beer.Id}");
        Assert.Equal(BeerAvailability.Available, afterAvailable!.Availability);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var bartender = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
            .FindByEmailAsync(SeedData.DevBartenderEmail);
        var audits = context.AdminAudits.Where(a => a.EntityId == beer.Id.ToString() && a.Action == "AvailabilityChange").ToList();
        Assert.Equal(2, audits.Count);
        Assert.All(audits, a => Assert.Equal(bartender!.Id, a.AdminUserId));
    }

    [Fact]
    public async Task AvailabilityFlip_WithWrongPin_RejectedTheSameGenericWayAsConfirmation()
    {
        var token = await RegisterCustomerAsync("availability-flip-wrongpin@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var beers = (await _client.GetFromJsonAsync<BeerSearchResponse>("/api/beers"))!.Items;
        var beer = beers![0];

        var wrongAvailabilityAttempt = await _client.PostAsJsonAsync("/api/confirmations/availability",
            new PinAvailabilityRequest(beer.Id, "000000", BeerAvailability.OutOfStock));
        Assert.Equal(HttpStatusCode.Unauthorized, wrongAvailabilityAttempt.StatusCode);

        var wrongConfirmationAttempt = await _client.PostAsJsonAsync("/api/confirmations",
            new ConfirmationRequest(beer.Id, "000000"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrongConfirmationAttempt.StatusCode);

        Assert.Equal(
            await wrongConfirmationAttempt.Content.ReadAsStringAsync(),
            await wrongAvailabilityAttempt.Content.ReadAsStringAsync());

        var stillOriginal = await _client.GetFromJsonAsync<BeerDetailResponse>($"/api/beers/{beer.Id}");
        Assert.NotEqual(BeerAvailability.OutOfStock, stillOriginal!.Availability);
    }

    // #74: rating requires an existing confirmation, is editable in place, and is
    // visible from beer detail (BeerDetailResponse.MyRating) — there's no My Beers
    // screen yet for it to live on instead.
    [Fact]
    public async Task Rating_RequiresConfirmation_ThenIsEditableAndVisibleFromBeerDetail()
    {
        var token = await RegisterCustomerAsync("rating-flow-customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var beers = (await _client.GetFromJsonAsync<BeerSearchResponse>("/api/beers"))!.Items;
        var beer = beers![0];

        var beforeConfirming = await _client.PutAsJsonAsync($"/api/me/ratings/{beer.Id}", new SetRatingRequest(4));
        Assert.Equal(HttpStatusCode.BadRequest, beforeConfirming.StatusCode);

        var confirmed = await _client.PostAsJsonAsync("/api/confirmations", new ConfirmationRequest(beer.Id, SeedData.DevBartenderPin));
        Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);

        var outOfRange = await _client.PutAsJsonAsync($"/api/me/ratings/{beer.Id}", new SetRatingRequest(6));
        Assert.Equal(HttpStatusCode.BadRequest, outOfRange.StatusCode);

        var firstRating = await _client.PutAsJsonAsync($"/api/me/ratings/{beer.Id}", new SetRatingRequest(4));
        Assert.Equal(HttpStatusCode.NoContent, firstRating.StatusCode);

        var afterFirstRating = await _client.GetFromJsonAsync<BeerDetailResponse>($"/api/beers/{beer.Id}");
        Assert.True(afterFirstRating!.Confirmed);
        Assert.Equal(4, afterFirstRating.MyRating);

        var revisedRating = await _client.PutAsJsonAsync($"/api/me/ratings/{beer.Id}", new SetRatingRequest(2));
        Assert.Equal(HttpStatusCode.NoContent, revisedRating.StatusCode);

        var afterRevision = await _client.GetFromJsonAsync<BeerDetailResponse>($"/api/beers/{beer.Id}");
        Assert.Equal(2, afterRevision!.MyRating);
    }

    // #74: a lightweight milestone at 100, distinct from the 200-beer mug.
    [Fact]
    public async Task ConfirmationLoop_ReachingMilestoneCount_ReportsMilestoneReached()
    {
        var token = await RegisterCustomerAsync("milestone-customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        ConfirmationResponse? last = null;
        for (var i = 0; i < ConfirmationsController.MilestoneCount; i++)
        {
            int beerId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var beer = new Beer { Name = $"Milestone Beer {i}", Brewery = "Test Brewery", Style = "Test Style" };
                context.Beers.Add(beer);
                await context.SaveChangesAsync();
                beerId = beer.Id;
            }

            var response = await _client.PostAsJsonAsync("/api/confirmations",
                new ConfirmationRequest(beerId, SeedData.DevBartenderPin));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            last = await response.Content.ReadFromJsonAsync<ConfirmationResponse>();
        }

        Assert.Equal(ConfirmationsController.MilestoneCount, last!.ConfirmedCount);
        Assert.True(last.MilestoneReached);
        Assert.False(last.MugEarned);
    }

    private async Task<string> RegisterCustomerAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!"));
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    public void Dispose() => _factory.Dispose();
}
