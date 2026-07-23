using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Xunit;

namespace BeerApi.Tests.Controllers;

// Role gating is middleware behavior covered by the HTTP-level AdminUsersTests;
// these tests cover the role-reassignment and audit-writing logic. UserManager/RoleManager
// are resolved from a small DI container (mirroring Program.cs's AddIdentity setup) rather
// than hand-constructed, since their constructors have many collaborators that are easy to
// wire wrong by hand.
public class AdminUsersControllerTests
{
    private const string AdminId = "admin-1";
    private const string TargetUserId = "target-1";

    private sealed class Fixture : IDisposable
    {
        public ApplicationDbContext Context { get; }
        public UserManager<ApplicationUser> UserManager { get; }
        public RoleManager<IdentityRole> RoleManager { get; }
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        public Fixture()
        {
            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddLogging();
            _provider = services.BuildServiceProvider();
            _scope = _provider.CreateScope();
            Context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            UserManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            RoleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        }

        public async Task SeedRolesAsync()
        {
            foreach (var role in new[] { "Admin", "Bartender", "Customer" })
            {
                await RoleManager.CreateAsync(new IdentityRole(role));
            }
        }

        public async Task<ApplicationUser> SeedTargetUserAsync(string currentRole = "Customer")
        {
            var user = new ApplicationUser { Id = TargetUserId, Email = "target@example.com", UserName = "target@example.com" };
            await UserManager.CreateAsync(user);
            await UserManager.AddToRoleAsync(user, currentRole);
            return user;
        }

        public async Task SeedActivePinAsync(string userId = TargetUserId)
        {
            Context.StaffPins.Add(new StaffPin { UserId = userId, PinHash = "hash", IsActive = true });
            await Context.SaveChangesAsync();
        }

        public AdminUsersController CreateController(string adminId = AdminId)
        {
            return new AdminUsersController(Context, UserManager, RoleManager)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, adminId) }, "Test")),
                    },
                },
            };
        }

        public void Dispose()
        {
            _scope.Dispose();
            _provider.Dispose();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AssignRole_WithoutReason_ReturnsBadRequest_AndChangesNothing(string? reason)
    {
        using var fixture = new Fixture();
        await fixture.SeedRolesAsync();
        await fixture.SeedTargetUserAsync();
        var controller = fixture.CreateController();

        var result = await controller.AssignRole(TargetUserId, new AssignRoleRequest("Bartender", reason!));

        Assert.IsType<BadRequestObjectResult>(result);
        var user = await fixture.UserManager.FindByIdAsync(TargetUserId);
        Assert.Contains("Customer", await fixture.UserManager.GetRolesAsync(user!));
        Assert.Empty(fixture.Context.AdminAudits);
    }

    [Fact]
    public async Task AssignRole_InvalidRole_ReturnsBadRequest_AndChangesNothing()
    {
        using var fixture = new Fixture();
        await fixture.SeedRolesAsync();
        await fixture.SeedTargetUserAsync();
        var controller = fixture.CreateController();

        var result = await controller.AssignRole(TargetUserId, new AssignRoleRequest("SuperAdmin", "promote"));

        Assert.IsType<BadRequestObjectResult>(result);
        var user = await fixture.UserManager.FindByIdAsync(TargetUserId);
        Assert.Contains("Customer", await fixture.UserManager.GetRolesAsync(user!));
        Assert.Empty(fixture.Context.AdminAudits);
    }

    [Fact]
    public async Task AssignRole_UnknownUserId_ReturnsNotFound()
    {
        using var fixture = new Fixture();
        await fixture.SeedRolesAsync();
        var controller = fixture.CreateController();

        var result = await controller.AssignRole("nonexistent", new AssignRoleRequest("Bartender", "promote"));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AssignRole_ReplacesRole_AndWritesAudit()
    {
        using var fixture = new Fixture();
        await fixture.SeedRolesAsync();
        await fixture.SeedTargetUserAsync(currentRole: "Customer");
        var controller = fixture.CreateController();

        var result = await controller.AssignRole(TargetUserId, new AssignRoleRequest("Bartender", "promoted to staff"));

        Assert.IsType<NoContentResult>(result);
        var user = await fixture.UserManager.FindByIdAsync(TargetUserId);
        var roles = await fixture.UserManager.GetRolesAsync(user!);
        Assert.Equal(new[] { "Bartender" }, roles);

        var audit = Assert.Single(fixture.Context.AdminAudits);
        Assert.Equal(AdminId, audit.AdminUserId);
        Assert.Equal("User", audit.EntityType);
        Assert.Equal(TargetUserId, audit.EntityId);
        Assert.Equal("RoleChange", audit.Action);
        Assert.Equal("Customer", audit.BeforeSnapshot);
        Assert.Equal("Bartender", audit.AfterSnapshot);
        Assert.Equal("promoted to staff", audit.Reason);
        Assert.True((DateTime.UtcNow - audit.CreatedAt).Duration() < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetUsers_ListsRoleActiveStatusAndPinPresence()
    {
        using var fixture = new Fixture();
        await fixture.SeedRolesAsync();
        await fixture.SeedTargetUserAsync(currentRole: "Bartender");
        await fixture.SeedActivePinAsync();
        var controller = fixture.CreateController();

        var result = await controller.GetUsers();

        var rows = Assert.IsAssignableFrom<IReadOnlyList<AdminUserResponse>>(result.Value);
        var row = Assert.Single(rows, r => r.Id == TargetUserId);
        Assert.Equal("target@example.com", row.Email);
        Assert.Equal("Bartender", row.Role);
        Assert.True(row.IsActive);
        Assert.True(row.HasActivePin);
    }

    [Fact]
    public async Task GetUsers_UserWithMultipleRoleRows_DoesNotThrow()
    {
        // AssignRole always leaves a user with exactly one role, but a manual DB
        // correction could leave more than one — the list must degrade gracefully
        // (pick one) rather than 500 the entire endpoint.
        using var fixture = new Fixture();
        await fixture.SeedRolesAsync();
        var user = await fixture.SeedTargetUserAsync(currentRole: "Customer");
        await fixture.UserManager.AddToRoleAsync(user, "Bartender");
        var controller = fixture.CreateController();

        var result = await controller.GetUsers();

        var rows = Assert.IsAssignableFrom<IReadOnlyList<AdminUserResponse>>(result.Value);
        var row = Assert.Single(rows, r => r.Id == TargetUserId);
        Assert.Contains(row.Role, new[] { "Customer", "Bartender" });
    }

    [Fact]
    public async Task GetUsers_DeactivatedUser_ReportsInactive_AndNoPin()
    {
        using var fixture = new Fixture();
        await fixture.SeedRolesAsync();
        await fixture.SeedTargetUserAsync();
        var controller = fixture.CreateController();
        await controller.DeactivateAccount(TargetUserId, new AccountActionRequest("policy violation"));

        var result = await controller.GetUsers();

        var rows = Assert.IsAssignableFrom<IReadOnlyList<AdminUserResponse>>(result.Value);
        var row = Assert.Single(rows, r => r.Id == TargetUserId);
        Assert.False(row.IsActive);
        Assert.False(row.HasActivePin);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeactivateAccount_WithoutReason_ReturnsBadRequest_AndChangesNothing(string? reason)
    {
        using var fixture = new Fixture();
        await fixture.SeedRolesAsync();
        await fixture.SeedTargetUserAsync();
        var controller = fixture.CreateController();

        var result = await controller.DeactivateAccount(TargetUserId, new AccountActionRequest(reason!));

        Assert.IsType<BadRequestObjectResult>(result);
        var reloaded = await fixture.UserManager.FindByIdAsync(TargetUserId);
        Assert.False(await fixture.UserManager.IsLockedOutAsync(reloaded!));
        Assert.Empty(fixture.Context.AdminAudits);
    }

    [Fact]
    public async Task DeactivateAccount_UnknownUserId_ReturnsNotFound()
    {
        using var fixture = new Fixture();
        await fixture.SeedRolesAsync();
        var controller = fixture.CreateController();

        var result = await controller.DeactivateAccount("nonexistent", new AccountActionRequest("policy violation"));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeactivateAccount_LocksOutUser_DeactivatesPin_AndWritesAudit()
    {
        using var fixture = new Fixture();
        await fixture.SeedRolesAsync();
        await fixture.SeedTargetUserAsync(currentRole: "Bartender");
        await fixture.SeedActivePinAsync();
        var controller = fixture.CreateController();

        var result = await controller.DeactivateAccount(TargetUserId, new AccountActionRequest("policy violation"));

        Assert.IsType<NoContentResult>(result);
        var reloaded = await fixture.UserManager.FindByIdAsync(TargetUserId);
        Assert.True(await fixture.UserManager.IsLockedOutAsync(reloaded!));
        var pin = await fixture.Context.StaffPins.FirstAsync(p => p.UserId == TargetUserId);
        Assert.False(pin.IsActive);

        var audit = Assert.Single(fixture.Context.AdminAudits);
        Assert.Equal(AdminId, audit.AdminUserId);
        Assert.Equal("User", audit.EntityType);
        Assert.Equal(TargetUserId, audit.EntityId);
        Assert.Equal("Deactivate", audit.Action);
        Assert.Equal("Active", audit.BeforeSnapshot);
        Assert.Equal("Deactivated", audit.AfterSnapshot);
        Assert.Equal("policy violation", audit.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReactivateAccount_WithoutReason_ReturnsBadRequest_AndChangesNothing(string? reason)
    {
        using var fixture = new Fixture();
        await fixture.SeedRolesAsync();
        await fixture.SeedTargetUserAsync();
        var controller = fixture.CreateController();
        await controller.DeactivateAccount(TargetUserId, new AccountActionRequest("policy violation"));

        var result = await controller.ReactivateAccount(TargetUserId, new AccountActionRequest(reason!));

        Assert.IsType<BadRequestObjectResult>(result);
        var reloaded = await fixture.UserManager.FindByIdAsync(TargetUserId);
        Assert.True(await fixture.UserManager.IsLockedOutAsync(reloaded!));
        Assert.Single(fixture.Context.AdminAudits);
    }

    [Fact]
    public async Task ReactivateAccount_UnknownUserId_ReturnsNotFound()
    {
        using var fixture = new Fixture();
        await fixture.SeedRolesAsync();
        var controller = fixture.CreateController();

        var result = await controller.ReactivateAccount("nonexistent", new AccountActionRequest("appeal approved"));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ReactivateAccount_ClearsLockout_DoesNotRestorePin_AndWritesAudit()
    {
        using var fixture = new Fixture();
        await fixture.SeedRolesAsync();
        await fixture.SeedTargetUserAsync(currentRole: "Bartender");
        await fixture.SeedActivePinAsync();
        var controller = fixture.CreateController();
        await controller.DeactivateAccount(TargetUserId, new AccountActionRequest("policy violation"));

        var result = await controller.ReactivateAccount(TargetUserId, new AccountActionRequest("appeal approved"));

        Assert.IsType<NoContentResult>(result);
        var reloaded = await fixture.UserManager.FindByIdAsync(TargetUserId);
        Assert.False(await fixture.UserManager.IsLockedOutAsync(reloaded!));
        var pin = await fixture.Context.StaffPins.FirstAsync(p => p.UserId == TargetUserId);
        Assert.False(pin.IsActive);

        var audit = Assert.Single(fixture.Context.AdminAudits, a => a.Action == "Reactivate");
        Assert.Equal("Deactivated", audit.BeforeSnapshot);
        Assert.Equal("Active", audit.AfterSnapshot);
        Assert.Equal("appeal approved", audit.Reason);
    }
}
