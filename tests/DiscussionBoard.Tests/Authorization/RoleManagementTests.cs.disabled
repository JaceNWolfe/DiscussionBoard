using System.Security.Claims;
using DiscussionBoard.Authorization;
using DiscussionBoard.Models;
using DiscussionBoard.Tests.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DiscussionBoard.Tests.Authorization;

public sealed class RoleManagementTests
{
    private static ClaimsPrincipal Admin(string id) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, id),
        new Claim(ClaimTypes.Role, ApplicationRoles.Admin)
    }, "Test"));

    [Fact]
    public async Task RoleChangesRejectSelfDemotionAndInvalidTargets()
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var service = scope.ServiceProvider.GetRequiredService<IApplicationAuthorizationService>();
        Assert.True((await roles.CreateAsync(new IdentityRole(ApplicationRoles.Admin))).Succeeded);
        var admin = new ApplicationUser { UserName = "admin@example.test", Email = "admin@example.test" };
        var target = new ApplicationUser { UserName = "target@example.test", Email = "target@example.test" };
        Assert.True((await users.CreateAsync(admin)).Succeeded);
        Assert.True((await users.CreateAsync(target)).Succeeded);
        Assert.True((await users.AddToRoleAsync(admin, ApplicationRoles.Admin)).Succeeded);
        var actor = Admin(admin.Id);
        Assert.False((await service.RemoveRoleAsync(actor, admin.Id, ApplicationRoles.Admin)).Succeeded);
        Assert.False((await service.AddRoleAsync(actor, target.Id, "InventedRole")).Succeeded);
        Assert.False((await service.AddRoleAsync(actor, "missing-user", ApplicationRoles.Admin)).Succeeded);
        Assert.True((await users.IsInRoleAsync(admin, ApplicationRoles.Admin)));
        Assert.True((await service.AddRoleAsync(actor, target.Id, ApplicationRoles.Admin)).Succeeded);
        Assert.True((await users.IsInRoleAsync(target, ApplicationRoles.Admin)));
        Assert.True((await service.RemoveRoleAsync(actor, target.Id, ApplicationRoles.Admin)).Succeeded);
        Assert.False((await users.IsInRoleAsync(target, ApplicationRoles.Admin)));
    }

    [Fact]
    public async Task NonAdminCannotGrantRoleEvenWhenTargetExists()
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var service = scope.ServiceProvider.GetRequiredService<IApplicationAuthorizationService>();
        Assert.True((await roles.CreateAsync(new IdentityRole(ApplicationRoles.Admin))).Succeeded);
        var target = new ApplicationUser { UserName = "target@example.test", Email = "target@example.test" };
        Assert.True((await users.CreateAsync(target)).Succeeded);
        var nonAdmin = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, target.Id) }, "Test"));
        Assert.False((await service.AddRoleAsync(nonAdmin, target.Id, ApplicationRoles.Admin)).Succeeded);
        Assert.False((await users.IsInRoleAsync(target, ApplicationRoles.Admin)));
    }
}
