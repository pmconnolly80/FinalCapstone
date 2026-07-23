using BeerApi.Data;
using BeerApi.Services;
using BeerApi.Tests.TestDoubles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BeerApi.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // Known secret so tests can build a validly-signed Facebook data-deletion callback
    // request without touching the real (empty-by-default) appsettings.json value.
    public const string FacebookAppSecret = "test-facebook-app-secret";

    private readonly string _databaseName = Guid.NewGuid().ToString();

    public FakeEmailSender EmailSender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Facebook:AppSecret"] = FacebookAppSecret,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);
        });
    }
}
