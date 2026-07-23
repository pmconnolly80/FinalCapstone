using BeerApi.Data;
using BeerApi.Models;
using BeerApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IBreweryLookupService, OpenBreweryDbService>(client =>
{
    client.BaseAddress = new Uri("https://api.openbrewerydb.org/v1/");
});
builder.Services.AddHttpClient<ICatalogBeerService, CatalogBeerService>(client =>
{
    client.BaseAddress = new Uri("https://api.catalog.beer/");
});

builder.Services.AddSingleton<ISmtpClientFactory, SmtpClientFactory>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IExternalLoginService, ExternalLoginService>();
builder.Services.AddScoped<IAccountDeletionService, AccountDeletionService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=beerdb;Username=beeruser;Password=beerpass";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Length-only policy (#17): one rule the register form can state plainly, instead
        // of the four hidden composition rules Identity defaults to. Keep in sync with the
        // hint and client-side check in frontend/src/pages/AuthPage.jsx.
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "development-secret-key-change-me";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "beer-api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "beer-client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
}).AddGoogle(googleOptions =>
{
    // Never the default/challenge scheme (Jwt stays that) — only reached via an explicit
    // Challenge(properties, "Google") call from AuthController's external-login endpoint.
    // SignInScheme is Identity's own external cookie, used purely to correlate the
    // redirect round-trip; it never becomes this app's session mechanism.
    googleOptions.SignInScheme = IdentityConstants.ExternalScheme;
    var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
    var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    googleOptions.ClientId = string.IsNullOrWhiteSpace(googleClientId) ? "placeholder" : googleClientId;
    googleOptions.ClientSecret = string.IsNullOrWhiteSpace(googleClientSecret) ? "placeholder" : googleClientSecret;
    googleOptions.ClaimActions.MapJsonKey("email_verified", "verified_email");
}).AddFacebook(facebookOptions =>
{
    facebookOptions.SignInScheme = IdentityConstants.ExternalScheme;
    var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
    var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
    facebookOptions.AppId = string.IsNullOrWhiteSpace(facebookAppId) ? "placeholder" : facebookAppId;
    facebookOptions.AppSecret = string.IsNullOrWhiteSpace(facebookAppSecret) ? "placeholder" : facebookAppSecret;
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        // EF Core's InMemory provider (used by BeerApi.Tests) doesn't support Migrate().
        db.Database.EnsureCreated();
    }
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.MapControllers();

app.Run();

public partial class Program { }
