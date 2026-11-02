using AngleSharp.Html.Parser;
using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DiscussionBoard.Tests.Infrastructure;

namespace DiscussionBoard.Tests.Authentication;

public sealed class AuthorRenderingWorkflowTests
{
    [Theory(DisplayName = "Discussion - author profile or legacy fallback is rendered")]
    [InlineData("Visible Author", "fox", "Visible Author", "fox.svg")]
    [InlineData("", "default", "Unconfigured User", "default.svg")]
    public async Task AuthoredPostRendersProfile(string name, string avatar, string expectedName, string expectedAvatar)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var factory = new RenderingFactory(connection);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        int id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();
            var user = new ApplicationUser { Id = "author-1", UserName = "author@example.test",
                DisplayName = name, AvatarId = avatar };
            var forum = new Forum { Name = "Profile tests" };
            db.Users.Add(user);
            db.Forums.Add(forum);
            await db.SaveChangesAsync();
            var thread = new DiscussionThread { ForumId = forum.Id, ApplicationUserId = user.Id,
                Title = "Test thread", Content = "Test thread content" };
            db.DiscussionThreads.Add(thread);
            await db.SaveChangesAsync();
            id = thread.Id;
            db.Posts.Add(new Post { DiscussionThreadId = id, ApplicationUserId = user.Id,
                Title = "Test reply", Content = "Unique reply body" });
            await db.SaveChangesAsync();
        }
        using var response = await client.GetAsync($"/Discussion/{id}");
        Assert.True(response.IsSuccessStatusCode, $"Discussion GET: {(int)response.StatusCode}");
        var doc = await new HtmlParser().ParseDocumentAsync(await response.Content.ReadAsStringAsync());
        var post = doc.QuerySelector("article[data-post-id]");
        Assert.NotNull(post);
        Assert.Contains(expectedName, post.TextContent);
        Assert.True(post.QuerySelectorAll("img").Any(x =>
            x.GetAttribute("src")?.Contains(expectedAvatar, StringComparison.OrdinalIgnoreCase) == true),
            $"Post author avatar must render {expectedAvatar}.");
    }

    private sealed class RenderingFactory(SqliteConnection connection) : WebApplicationFactory<DiscussionBoard.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName, _ => { });
            });
        }
    }
}
