using System.Reflection;
using DiscussionBoard.Data;
using DiscussionBoard.Models;
using DiscussionBoard.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DiscussionBoard.Tests.Authentication;

/// <summary>
/// Black-box checks of the documented profile-service contract using real SQLite queries.
/// No specific LINQ expression, PageModel implementation, or internal helper is required.
/// </summary>
public sealed class ProfileServiceBehaviorTests
{
    [Theory]
    [InlineData("available", null, true)]
    [InlineData("Alice", null, false)]
    [InlineData("aLiCe", null, false)]
    [InlineData("  ALICE  ", null, false)]
    [InlineData("Alice", "user-1", true)]
    [InlineData("Alice", "user-2", false)]
    [InlineData("", null, false)]
    [InlineData(" ", null, false)]
    [InlineData("a", null, false)]
    public async Task AvailabilityMatchesContract(string candidate, string? excludeUserId, bool expected)
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        await using var db = fixture.CreateContext();

        // These are test fixtures, not user-facing email addresses.
        db.Users.Add(new ApplicationUser
        {
            Id = "user-1",
            UserName = "first@example.test",
            NormalizedUserName = "FIRST@EXAMPLE.TEST"
        });
        db.Users.Add(new ApplicationUser
        {
            Id = "user-2",
            UserName = "second@example.test",
            NormalizedUserName = "SECOND@EXAMPLE.TEST"
        });

        // Set the assigned property reflectively so this test project can compile
        // before students implement the feature.
        var displayName = typeof(ApplicationUser).GetProperty("DisplayName");
        Assert.NotNull(displayName);
        displayName.SetValue(db.Users.Local.First(x => x.Id == "user-1"), "Alice");
        displayName.SetValue(db.Users.Local.First(x => x.Id == "user-2"), "Bob");
        await db.SaveChangesAsync();

        var type = typeof(ApplicationUser).Assembly
            .GetType("DiscussionBoard.Services.UserProfileService");
        Assert.NotNull(type);

        // A constructor taking ApplicationDbContext is natural but not mandatory:
        // resolve through DI so additional valid dependencies are supported.
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton(db);
        services.AddScoped<ApplicationDbContext>(_ => db);
        services.AddScoped(type);
        using var provider = services.BuildServiceProvider();
        object instance;
        try { instance = provider.GetRequiredService(type); }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Could not instantiate UserProfileService for behavior testing: {ex.Message}", ex);
        }

        var method = type.GetMethod("IsDisplayNameAvailableAsync",
            new[] { typeof(string), typeof(string) });
        Assert.NotNull(method);
        var task = method.Invoke(instance, new object?[] { candidate, excludeUserId })
            as Task<bool>;
        Assert.NotNull(task);
        Assert.Equal(expected, await task);
    }

    [Fact]
    public async Task RejectsDisplayNameLongerThanSixtyCharacters()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        await using var db = fixture.CreateContext();
        var type = typeof(ApplicationUser).Assembly
            .GetType("DiscussionBoard.Services.UserProfileService");
        Assert.NotNull(type);
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddScoped<ApplicationDbContext>(_ => db);
        services.AddScoped(type);
        using var provider = services.BuildServiceProvider();
        var instance = provider.GetRequiredService(type);
        var method = type.GetMethod("IsDisplayNameAvailableAsync",
            new[] { typeof(string), typeof(string) });
        Assert.NotNull(method);
        var task = method.Invoke(instance, new object?[] { new string('x', 61), null })
            as Task<bool>;
        Assert.NotNull(task);
        Assert.False(await task);
    }
}
