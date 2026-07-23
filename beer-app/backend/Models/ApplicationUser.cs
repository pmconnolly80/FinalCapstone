using Microsoft.AspNetCore.Identity;

namespace BeerApi.Models;

public class ApplicationUser : IdentityUser
{
    public bool MarketingConsent { get; set; } = false;
}
