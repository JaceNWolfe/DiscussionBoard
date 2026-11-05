using System.Security.Claims;
using DiscussionBoard.Authorization;
using DiscussionBoard.Models;
using DiscussionBoard.Tests.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DiscussionBoard.Tests.Authorization;

public sealed class RoleMutationSecurityTests
{
    private static ClaimsPrincipal Principal(string id, bool admin) => new(new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.NameIdentifier, id) }.Concat(admin
            ? new[] { new Claim(ClaimTypes.Role, ApplicationRoles.Admin) }
            : Array.Empty<Claim>()), "Test"));

    [Fact]
    public async Task LastAdminAndSelfDemotionAreRejectedWithoutChangingMembership()
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var service = scope.ServiceProvider.GetRequiredService<IApplicationAuthorizationService>();
        Assert.True((await roles.CreateAsync(new IdentityRole(ApplicationRoles.Admin))).Succeeded);
        var sole = new ApplicationUser { UserName = "sole-admin@example.test", Email = "sole-admin@example.test" };
        var ordinary = new ApplicationUser { UserName = "ordinary@example.test", Email = "ordinary@example.test" };
        Assert.True((await users.CreateAsync(sole)).Succeeded);
        Assert.True((await users.CreateAsync(ordinary)).Succeeded);
        Assert.True((await users.AddToRoleAsync(sole, ApplicationRoles.Admin)).Succeeded);
        Assert.False((await service.RemoveRoleAsync(Principal(sole.Id, true), sole.Id, ApplicationRoles.Admin)).Succeeded);
        Assert.True(await users.IsInRoleAsync(sole, ApplicationRoles.Admin));
        Assert.False((await service.RemoveRoleAsync(Principal(ordinary.Id, false), sole.Id, ApplicationRoles.Admin)).Succeeded);
        Assert.True(await users.IsInRoleAsync(sole, ApplicationRoles.Admin));
        Assert.False((await service.AddRoleAsync(Principal(ordinary.Id, true), ordinary.Id, ApplicationRoles.Admin)).Succeeded);
        Assert.False(await users.IsInRoleAsync(ordinary, ApplicationRoles.Admin));
    }
}
