using System.Net;
using DiscussionBoard.Tests.Authentication;

namespace DiscussionBoard.Tests.Authorization;

/// <summary>HTTP-level checks using the application's real authorization middleware.</summary>
public sealed class HttpAuthorizationTests
{
    [Theory]
    [InlineData("/Logs")]
    [InlineData("/Admin/Users")]
    public async Task AdminOnlyPagesChallengeAnonymousAndForbidRegularUsers(string path)
    {
        using var factory = new AuthenticationWebTestFactory(enableTestUser: true);
        await factory.InitializeDatabaseAsync();
        using var anonymous = factory.Client();
        using var regular = factory.AuthenticatedClient("regular-test-user");
        using var anonymousResponse = await anonymous.GetAsync(path);
        using var regularResponse = await regular.GetAsync(path);
        Assert.True(anonymousResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Unauthorized,
            $"Expected anonymous challenge at {path}, received {anonymousResponse.StatusCode}");
        Assert.True(regularResponse.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Redirect,
            $"Expected access denial at {path}, received {regularResponse.StatusCode}");
        if (regularResponse.StatusCode == HttpStatusCode.Redirect)
            Assert.Contains("AccessDenied", regularResponse.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/Logs")]
    [InlineData("/Admin/Users")]
    public async Task AdminCanOpenProtectedPages(string path)
    {
        using var factory = new AuthenticationWebTestFactory(enableTestUser: true);
        await factory.InitializeDatabaseAsync();
        using var admin = factory.AuthenticatedClient("admin-test-user");
        admin.DefaultRequestHeaders.Add(TestUserAuthenticationHandler.RoleHeader, "Admin");
        using var response = await admin.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Thread/Edit/999999")]
    [InlineData("/Discussion/999999/Post/Edit/999999")]
    public async Task AnonymousEditRoutesChallengeBeforeResourceLookup(string path)
    {
        using var factory = new AuthenticationWebTestFactory(enableTestUser: true);
        await factory.InitializeDatabaseAsync();
        using var anonymous = factory.Client();
        using var response = await anonymous.GetAsync(path);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Unauthorized,
            $"Expected anonymous challenge at {path}, received {response.StatusCode}");
    }
}
