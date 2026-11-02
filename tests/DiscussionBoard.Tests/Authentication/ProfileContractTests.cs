using System.Reflection;
using DiscussionBoard.Models;
using DiscussionBoard.Services;

namespace DiscussionBoard.Tests.Authentication;

/// <summary>
/// Tests explicit assignment contracts, without depending on Razor Page implementation.
/// These tests intentionally fail until the student implements the profile feature.
/// </summary>
public sealed class ProfileContractTests
{
    [Theory]
    [InlineData("DisplayName")]
    [InlineData("AvatarId")]
    public void ApplicationUser_ExposesEditableStringProperty(string propertyName)
    {
        var property = typeof(ApplicationUser).GetProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
        Assert.True(property.CanRead && property.CanWrite);
    }

    [Fact]
    public void ProfileService_ExposesTheAssignedAsyncContract()
    {
        var assembly = typeof(AvatarCatalog).Assembly;
        var contract = assembly.GetType("DiscussionBoard.Services.IUserProfileService");
        var implementation = assembly.GetType("DiscussionBoard.Services.UserProfileService");
        Assert.NotNull(contract);
        Assert.NotNull(implementation);
        Assert.True(contract.IsInterface);
        Assert.True(contract.IsAssignableFrom(implementation));

        var method = contract.GetMethod("IsDisplayNameAvailableAsync",
            new[] { typeof(string), typeof(string) });
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<bool>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.True(parameters[1].IsOptional,
            "excludeUserId must be optional, so registration can omit it.");
    }
}
