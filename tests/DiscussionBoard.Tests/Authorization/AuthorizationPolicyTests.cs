using System.Security.Claims;
using DiscussionBoard.Authorization;
using DiscussionBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using DiscussionBoard.Tests.Authentication;

namespace DiscussionBoard.Tests.Authorization;

public sealed class AuthorizationPolicyTests
{
    private static ClaimsPrincipal Principal(string? id, bool admin = false)
    {
        if (id is null) return new ClaimsPrincipal(new ClaimsIdentity());
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id) };
        if (admin) claims.Add(new Claim(ClaimTypes.Role, ApplicationRoles.Admin));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Theory]
    [InlineData("owner", false, true)]
    [InlineData("stranger", false, false)]
    [InlineData("stranger", true, true)]
    [InlineData(null, false, false)]
    public async Task ThreadPolicyHonorsOwnerOrAdmin(string? id, bool admin, bool allowed)
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var resource = new DiscussionThread { ApplicationUserId = "owner" };
        Assert.Equal(allowed, (await service.AuthorizeAsync(Principal(id, admin), resource, ApplicationAuthorizationPolicies.IsOwnerOrAdmin)).Succeeded);
    }

    [Theory]
    [InlineData("owner", false, true)]
    [InlineData("stranger", false, false)]
    [InlineData("stranger", true, true)]
    [InlineData(null, false, false)]
    public async Task PostPolicyHonorsOwnerOrAdmin(string? id, bool admin, bool allowed)
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var resource = new Post { ApplicationUserId = "owner" };
        Assert.Equal(allowed, (await service.AuthorizeAsync(Principal(id, admin), resource, ApplicationAuthorizationPolicies.IsOwnerOrAdmin)).Succeeded);
    }

    [Fact]
    public async Task UnauthenticatedPrincipalCannotChangeRoles()
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IApplicationAuthorizationService>();
        Assert.False((await service.AddRoleAsync(Principal(null), "missing", ApplicationRoles.Admin)).Succeeded);
        Assert.False((await service.RemoveRoleAsync(Principal(null), "missing", ApplicationRoles.Admin)).Succeeded);
    }
}
