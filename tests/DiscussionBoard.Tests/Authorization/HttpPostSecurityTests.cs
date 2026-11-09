using System.Net;
using System.Text.RegularExpressions;
using DiscussionBoard.Data;
using DiscussionBoard.Models;
using DiscussionBoard.Tests.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiscussionBoard.Tests.Authorization;

/// <summary>End-to-end Razor Pages form submissions with real antiforgery tokens and persisted-state assertions.</summary>
public sealed class HttpPostSecurityTests
{
    private static async Task<(int threadId, int postId)> SeedDiscussionAsync(AuthenticationWebTestFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Users.AddRange(
            new ApplicationUser { Id = "http-owner", UserName = "http-owner", Email = "owner@example.test" },
            new ApplicationUser { Id = "http-other", UserName = "http-other", Email = "other@example.test" });
        var forum = new Forum { Name = "HTTP security forum" };
        var thread = new DiscussionThread { Forum = forum, ApplicationUserId = "http-owner", Title = "Original thread", Content = "Original thread content" };
        var post = new Post { DiscussionThread = thread, ApplicationUserId = "http-owner", Title = "Original post", Content = "Original post content" };
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        return (thread.Id, post.Id);
    }

    private static async Task<string> TokenAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(html, "value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"", RegexOptions.IgnoreCase);
        Assert.True(match.Success, $"Antiforgery token not found in {path}");
        return System.Net.WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path, string token, params (string key, string value)[] fields)
    {
        var values = new List<KeyValuePair<string, string>> { new("__RequestVerificationToken", token) };
        values.AddRange(fields.Select(x => new KeyValuePair<string, string>(x.key, x.value)));
        return await client.PostAsync(path, new FormUrlEncodedContent(values));
    }

    private static bool IsDenied(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.Forbidden ||
        response.StatusCode == HttpStatusCode.Redirect &&
        (response.Headers.Location?.ToString().Contains("AccessDenied", StringComparison.OrdinalIgnoreCase) ?? false);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task OwnerAndAdminPostEditsPersistOnlyEditableFields(bool post, bool admin)
    {
        using var factory = new AuthenticationWebTestFactory(enableTestUser: true);
        await factory.InitializeDatabaseAsync();
        var (threadId, postId) = await SeedDiscussionAsync(factory);
        var path = post ? $"/Discussion/{threadId}/Post/Edit/{postId}" : $"/Thread/Edit/{threadId}";
        using var client = factory.AuthenticatedClient(admin ? "http-other" : "http-owner");
        if (admin) client.DefaultRequestHeaders.Add(TestUserAuthenticationHandler.RoleHeader, "Admin");
        var token = await TokenAsync(client, path);
        using var response = await PostAsync(client, path, token,
            ("Input.Title", "Edited title"), ("Input.Content", "Edited content"),
            ("ApplicationUserId", "http-other"), ("Input.ApplicationUserId", "http-other"),
            ("ForumId", "999999"), ("Input.ForumId", "999999"),
            ("DiscussionThreadId", "999999"), ("Input.DiscussionThreadId", "999999"),
            ("ParentPostId", "999999"), ("Input.ParentPostId", "999999"),
            ("IsDeleted", "true"), ("Input.IsDeleted", "true"),
            ("CreatedAt", "2035-01-01T00:00:00Z"), ("Input.CreatedAt", "2035-01-01T00:00:00Z"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (post)
        {
            var saved = await db.Posts.AsNoTracking().SingleAsync(x => x.Id == postId);
            Assert.Equal("Edited title", saved.Title);
            Assert.Equal("Edited content", saved.Content);
            Assert.Equal("http-owner", saved.ApplicationUserId);
            Assert.Equal(threadId, saved.DiscussionThreadId);
            Assert.Null(saved.ParentPostId);
            Assert.False(saved.IsDeleted);
            Assert.NotEqual(2035, saved.CreatedAt.Year);
        }
        else
        {
            var saved = await db.DiscussionThreads.AsNoTracking().SingleAsync(x => x.Id == threadId);
            Assert.Equal("Edited title", saved.Title);
            Assert.Equal("Edited content", saved.Content);
            Assert.Equal("http-owner", saved.ApplicationUserId);
            Assert.NotEqual(999999, saved.ForumId);
            Assert.False(saved.IsDeleted);
            Assert.NotEqual(2035, saved.CreatedAt.Year);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NonownerPostIsDeniedAndLeavesDatabaseUnchanged(bool post)
    {
        using var factory = new AuthenticationWebTestFactory(enableTestUser: true);
        await factory.InitializeDatabaseAsync();
        var (threadId, postId) = await SeedDiscussionAsync(factory);
        var path = post ? $"/Discussion/{threadId}/Post/Edit/{postId}" : $"/Thread/Edit/{threadId}";
        using var other = factory.AuthenticatedClient("http-other");
        // Antiforgery is bound to the authenticated principal. Obtain a valid token for the
        // nonowner from another writable form, then submit to the protected edit endpoint.
        // Use the existing thread-creation form, which is available to any signed-in user.
        var otherToken = await TokenAsync(other, $"/Thread/Create/{await ForumIdAsync(factory, threadId)}");
        using var response = await PostAsync(other, path, otherToken,
            ("Input.Title", "Unauthorized edit"), ("Input.Content", "Unauthorized content"));
        Assert.True(IsDenied(response), $"Expected access denial, received {response.StatusCode}");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (post)
        {
            var saved = await db.Posts.AsNoTracking().SingleAsync(x => x.Id == postId);
            Assert.Equal("Original post", saved.Title);
            Assert.Equal("Original post content", saved.Content);
        }
        else
        {
            var saved = await db.DiscussionThreads.AsNoTracking().SingleAsync(x => x.Id == threadId);
            Assert.Equal("Original thread", saved.Title);
            Assert.Equal("Original thread content", saved.Content);
        }
    }

    private static async Task<int> ForumIdAsync(AuthenticationWebTestFactory factory, int threadId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .DiscussionThreads.Where(x => x.Id == threadId).Select(x => x.ForumId).SingleAsync();
    }

    private static async Task SeedAdminsAsync(AuthenticationWebTestFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.True((await roles.CreateAsync(new IdentityRole("Admin"))).Succeeded);
        foreach (var id in new[] { "http-admin-one", "http-admin-two", "http-regular" })
        {
            Assert.True((await manager.CreateAsync(new ApplicationUser
            { Id = id, UserName = id, Email = $"{id}@example.test" })).Succeeded);
        }
        foreach (var id in new[] { "http-admin-one", "http-admin-two" })
            Assert.True((await manager.AddToRoleAsync((await manager.FindByIdAsync(id))!, "Admin")).Succeeded);
    }

    [Fact]
    public async Task AdminRolePostsAddRemoveAndRejectSelfDemotion()
    {
        using var factory = new AuthenticationWebTestFactory(enableTestUser: true);
        await factory.InitializeDatabaseAsync();
        await SeedAdminsAsync(factory);
        using var admin = factory.AuthenticatedClient("http-admin-one");
        admin.DefaultRequestHeaders.Add(TestUserAuthenticationHandler.RoleHeader, "Admin");
        const string path = "/Admin/Users";
        var token = await TokenAsync(admin, path);
        using (var added = await PostAsync(admin, path + "?handler=Add", token, ("SelectedUserId", "http-regular")))
            Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True(await manager.IsInRoleAsync((await manager.FindByIdAsync("http-regular"))!, "Admin"));
        }
        using (var removed = await PostAsync(admin, path + "?handler=Remove", token, ("SelectedUserId", "http-regular")))
            Assert.Equal(HttpStatusCode.Redirect, removed.StatusCode);
        using (var self = await PostAsync(admin, path + "?handler=Remove", token, ("SelectedUserId", "http-admin-one")))
            Assert.Equal(HttpStatusCode.Redirect, self.StatusCode);
        using var checkScope = factory.Services.CreateScope();
        var users = checkScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.False(await users.IsInRoleAsync((await users.FindByIdAsync("http-regular"))!, "Admin"));
        Assert.True(await users.IsInRoleAsync((await users.FindByIdAsync("http-admin-one"))!, "Admin"));
    }

    [Fact]
    public async Task NonadminCannotSubmitRoleMutation()
    {
        using var factory = new AuthenticationWebTestFactory(enableTestUser: true);
        await factory.InitializeDatabaseAsync();
        await SeedAdminsAsync(factory);
        using var admin = factory.AuthenticatedClient("http-admin-one");
        admin.DefaultRequestHeaders.Add(TestUserAuthenticationHandler.RoleHeader, "Admin");
        using var regular = factory.AuthenticatedClient("http-regular");
        // A valid token for this principal isolates authorization from antiforgery rejection.
        var (threadId, _) = await SeedDiscussionAsync(factory);
        var token = await TokenAsync(regular, $"/Thread/Create/{await ForumIdAsync(factory, threadId)}");
        using var response = await PostAsync(regular, "/Admin/Users?handler=Add", token,
            ("SelectedUserId", "http-regular"));
        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden or HttpStatusCode.Redirect);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.False(await users.IsInRoleAsync((await users.FindByIdAsync("http-regular"))!, "Admin"));
    }
    [Fact]
    public async Task LastAdministratorCannotBeRemovedThroughHttpPost()
    {
        using var factory = new AuthenticationWebTestFactory(enableTestUser: true);
        await factory.InitializeDatabaseAsync();
        await SeedAdminsAsync(factory);
        using (var scope = factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var second = (await manager.FindByIdAsync("http-admin-two"))!;
            Assert.True((await manager.RemoveFromRoleAsync(second, "Admin")).Succeeded);
        }
        using var admin = factory.AuthenticatedClient("http-admin-one");
        admin.DefaultRequestHeaders.Add(TestUserAuthenticationHandler.RoleHeader, "Admin");
        var token = await TokenAsync(admin, "/Admin/Users");
        using var response = await PostAsync(admin, "/Admin/Users?handler=Remove", token,
            ("SelectedUserId", "http-admin-one"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope2 = factory.Services.CreateScope();
        var users = scope2.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True(await users.IsInRoleAsync((await users.FindByIdAsync("http-admin-one"))!, "Admin"));
        Assert.Single(await users.GetUsersInRoleAsync("Admin"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnonymousEditPostCannotModifyDatabase(bool post)
    {
        using var factory = new AuthenticationWebTestFactory(enableTestUser: true);
        await factory.InitializeDatabaseAsync();
        var (threadId, postId) = await SeedDiscussionAsync(factory);
        var path = post ? $"/Discussion/{threadId}/Post/Edit/{postId}" : $"/Thread/Edit/{threadId}";
        using var anonymous = factory.Client();
        // Without an antiforgery token, middleware may reject the request before authorization.
        using var response = await anonymous.PostAsync(path, new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.Title", "Anonymous tampering"),
            new KeyValuePair<string, string>("Input.Content", "Anonymous tampering")
        }));
        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Redirect,
            $"Unexpected anonymous POST response: {response.StatusCode}");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (post)
        {
            var saved = await db.Posts.AsNoTracking().SingleAsync(x => x.Id == postId);
            Assert.Equal("Original post", saved.Title);
            Assert.Equal("Original post content", saved.Content);
        }
        else
        {
            var saved = await db.DiscussionThreads.AsNoTracking().SingleAsync(x => x.Id == threadId);
            Assert.Equal("Original thread", saved.Title);
            Assert.Equal("Original thread content", saved.Content);
        }
    }

    [Fact]
    public async Task AnonymousRoleMutationCannotGrantAdmin()
    {
        using var factory = new AuthenticationWebTestFactory(enableTestUser: true);
        await factory.InitializeDatabaseAsync();
        await SeedAdminsAsync(factory);
        using var anonymous = factory.Client();
        using var response = await anonymous.PostAsync("/Admin/Users?handler=Add",
            new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("SelectedUserId", "http-regular") }));
        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Redirect);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.False(await users.IsInRoleAsync((await users.FindByIdAsync("http-regular"))!, "Admin"));
    }

}
