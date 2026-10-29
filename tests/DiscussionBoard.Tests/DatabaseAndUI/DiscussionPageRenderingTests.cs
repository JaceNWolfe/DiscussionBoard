using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DiscussionBoard.Tests.DatabaseAndUI
{
    public sealed class DiscussionPageRenderingTests
    {
        [Fact(DisplayName = "Discussion page - replies are nested under their immediate parent posts")]
        public async Task RepliesAreNestedUnderTheirImmediateParentPosts()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);

            await using var factory = new TestWebFactory(connection);
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });

            int threadId;
            int parentAId;
            int replyA1Id;
            int replyA11Id;
            int parentBId;

            // Seed an in-memory database with the hierarchy
            using (IServiceScope scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

                var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "nested-test" };
                var forum = new Forum { Name = "Hierarchy test forum" };
                db.Users.Add(user);
                db.Forums.Add(forum);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);

                var thread = new DiscussionThread
                {
                    ForumId = forum.Id, ApplicationUserId = user.Id,
                    Title = "Hierarchy test thread", Content = "Integration test"
                };
                db.DiscussionThreads.Add(thread);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                threadId = thread.Id;

                var parentA = new Post
                {
                    DiscussionThreadId = threadId, ApplicationUserId = user.Id,
                    Title = "Parent A", Content = "Parent A content"
                };
                var parentB = new Post
                {
                    DiscussionThreadId = threadId, ApplicationUserId = user.Id,
                    Title = "Parent B", Content = "Parent B content"
                };
                db.Posts.AddRange(parentA, parentB);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                parentAId = parentA.Id;
                parentBId = parentB.Id;

                var replyA1 = new Post
                {
                    DiscussionThreadId = threadId, ApplicationUserId = user.Id,
                    ParentPostId = parentAId, Title = "Reply A1", Content = "Reply A1 content"
                };
                db.Posts.Add(replyA1);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                replyA1Id = replyA1.Id;

                var replyA11 = new Post
                {
                    DiscussionThreadId = threadId, ApplicationUserId = user.Id,
                    ParentPostId = replyA1Id, Title = "Reply A1.1", Content = "Reply A1.1 content"
                };
                db.Posts.Add(replyA11);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                replyA11Id = replyA11.Id;
            }

            using var response = await client.GetAsync($"/Discussion/{threadId}", TestContext.Current.CancellationToken);
            Assert.True(response.IsSuccessStatusCode,
                $"Discussion page returned HTTP {(int)response.StatusCode}.");

            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            IElement parentAElement = FindPost(document, parentAId);
            IElement replyA1Element = FindPost(document, replyA1Id);
            IElement replyA11Element = FindPost(document, replyA11Id);
            IElement parentBElement = FindPost(document, parentBId);

            Assert.Null(ClosestAncestorPost(parentAElement));
            Assert.Equal(parentAId.ToString(), ClosestAncestorPost(replyA1Element)?.GetAttribute("data-post-id"));
            Assert.Equal(replyA1Id.ToString(), ClosestAncestorPost(replyA11Element)?.GetAttribute("data-post-id"));
            Assert.Null(ClosestAncestorPost(parentBElement));
        }

        private static IElement FindPost(IDocument document, int id)
        {
            var matches = document.QuerySelectorAll($"article[data-post-id='{id}']");
            return Assert.Single(matches);
        }

        private static IElement? ClosestAncestorPost(IElement element) =>
            element.ParentElement?.Closest("article[data-post-id]");

        private sealed class TestWebFactory(SqliteConnection connection) : WebApplicationFactory<DiscussionBoard.Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
                });
            }
        }
    }
}
