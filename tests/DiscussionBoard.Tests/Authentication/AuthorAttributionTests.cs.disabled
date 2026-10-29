using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiscussionBoard.Tests.Authentication;

/// <summary>Tests visible attribution in forum listings, discussion headers, and individual posts.</summary>
public sealed class AuthorAttributionTests
{
    [Fact(DisplayName = "Forum listing attributes each thread to its own author")]
    public async Task ForumListingShowsCorrectThreadAuthors()
    {
        using var fixture = await Fixture.CreateAsync();
        var doc = await fixture.GetAsync($"/Forum/{fixture.ForumId}");
        AssertAuthor(FindThread(doc, fixture.AliceThreadTitle), "Alice Explorer", "fox");
        AssertAuthor(FindThread(doc, fixture.BobThreadTitle), "Bob Explorer", "robot");
    }

    [Fact(DisplayName = "Discussion header attributes the original thread to its creator")]
    public async Task DiscussionHeaderShowsThreadCreator()
    {
        using var fixture = await Fixture.CreateAsync();
        var doc = await fixture.GetAsync($"/Discussion/{fixture.ThreadId}");
        // Exclude reply content; attribution must belong to the original discussion.
        var content = doc.QuerySelector("main") ?? doc.Body!;
        var discussionText = string.Join(" ", content.ChildNodes
            .Where(n => n is not IElement e || !e.Matches("section[aria-label='Discussion posts']"))
            .Select(n => n.TextContent));
        Assert.Contains("Alice Explorer", discussionText);
        Assert.True(content.QuerySelectorAll("img").Any(img =>
            !IsInsidePost(img) && HasAvatar(img, "fox")),
            "Discussion creator's avatar must appear outside the reply/post tree.");
    }

    [Fact(DisplayName = "Nested replies display their own authors, not their parent's author")]
    public async Task NestedRepliesAreAttributedIndependently()
    {
        using var fixture = await Fixture.CreateAsync();
        var doc = await fixture.GetAsync($"/Discussion/{fixture.ThreadId}");
        var parent = FindPost(doc, fixture.ParentPostId);
        var reply = FindPost(doc, fixture.ReplyPostId);
        AssertAuthor(parent, "Alice Explorer", "fox");
        AssertAuthor(reply, "Bob Explorer", "robot");
        Assert.DoesNotContain("Bob Explorer", OwnText(parent));
        Assert.DoesNotContain("Alice Explorer", OwnText(reply));
    }

    [Fact(DisplayName = "Existing discussions and posts reflect updated author profiles")]
    public async Task ExistingContentUsesCurrentProfile()
    {
        using var fixture = await Fixture.CreateAsync();
        await fixture.ChangeAliceAsync("Alice Renamed", "owl");
        var forum = await fixture.GetAsync($"/Forum/{fixture.ForumId}");
        AssertAuthor(FindThread(forum, fixture.AliceThreadTitle), "Alice Renamed", "owl");
        var discussion = await fixture.GetAsync($"/Discussion/{fixture.ThreadId}");
        AssertAuthor(FindPost(discussion, fixture.ParentPostId), "Alice Renamed", "owl");
        AssertAuthor(FindPost(discussion, fixture.ReplyPostId), "Bob Explorer", "robot");
    }

    [Fact(DisplayName = "Legacy accounts use the specified author-name and avatar fallback")]
    public async Task LegacyProfileFallbackAppearsInThreadAndPost()
    {
        using var fixture = await Fixture.CreateAsync();
        var forum = await fixture.GetAsync($"/Forum/{fixture.ForumId}");
        AssertAuthor(FindThread(forum, fixture.LegacyThreadTitle), "Unconfigured User", "default");
        var discussion = await fixture.GetAsync($"/Discussion/{fixture.ThreadId}");
        AssertAuthor(FindPost(discussion, fixture.LegacyPostId), "Unconfigured User", "default");
    }

    private static IElement FindThread(IDocument doc, string title)
    {
        var candidate = doc.QuerySelectorAll("article").FirstOrDefault(article =>
            article.QuerySelectorAll("a").Any(link => link.TextContent.Trim() == title));
        Assert.True(candidate is not null, $"Could not find thread card for '{title}'.");
        return candidate!;
    }

    private static IElement FindPost(IDocument doc, int id)
    {
        var candidate = doc.QuerySelector($"article[data-post-id='{id}']");
        Assert.True(candidate is not null, $"Could not find rendered post {id}.");
        return candidate!;
    }

    // Nested article descendants belong to replies; don't credit their content to the parent.
    private static string OwnText(IElement element) => string.Join(" ", element.ChildNodes
        .Where(n => n is not IElement e || !e.Matches("article, div[aria-label='Replies to this post']"))
        .Select(n => n.TextContent));

    private static void AssertAuthor(IElement container, string name, string avatar)
    {
        Assert.Contains(name, OwnText(container));
        Assert.True(container.QuerySelectorAll("img").Any(img =>
            !img.Ancestors().OfType<IElement>()
                .TakeWhile(a => a != container)
                .Any(a => a.Matches("article[data-post-id]"))
            && HasAvatar(img, avatar)), $"Expected '{avatar}' avatar for '{name}'.");
    }

    private static bool HasAvatar(IElement img, string avatar)
    {
        var src = img.GetAttribute("src") ?? "";
        return src.EndsWith($"/{avatar}.svg", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInsidePost(IElement img) =>
        img.Ancestors().OfType<IElement>().Any(e => e.Matches("article[data-post-id]"));

    private sealed class Fixture : IDisposable
    {
        private readonly AuthenticationWebTestFactory _factory;
        private readonly HttpClient _client;
        private readonly string _aliceId;
        public int ForumId { get; private set; }
        public int ThreadId { get; private set; }
        public int ParentPostId { get; private set; }
        public int ReplyPostId { get; private set; }
        public int LegacyPostId { get; private set; }
        public string AliceThreadTitle { get; } = "Unique Alice Thread";
        public string BobThreadTitle { get; } = "Unique Bob Thread";
        public string LegacyThreadTitle { get; } = "Unique Legacy Thread";

        private Fixture(AuthenticationWebTestFactory factory, HttpClient client, string aliceId)
        { _factory = factory; _client = client; _aliceId = aliceId; }

        public static async Task<Fixture> CreateAsync()
        {
            var factory = new AuthenticationWebTestFactory(enableTestUser: true);
            await factory.InitializeDatabaseAsync();
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var alice = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "alice@test.invalid", Email = "alice@test.invalid", DisplayName = "Alice Explorer", AvatarId = "fox" };
            var bob = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "bob@test.invalid", Email = "bob@test.invalid", DisplayName = "Bob Explorer", AvatarId = "robot" };
            var legacy = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "legacy@test.invalid", Email = "legacy@test.invalid", DisplayName = "", AvatarId = "" };
            db.Users.AddRange(alice, bob, legacy);
            var forum = new Forum { Name = "Attribution Tests", Description = "Author rendering integration tests" };
            db.Forums.Add(forum);
            await db.SaveChangesAsync();
            var thread = new DiscussionThread { ForumId = forum.Id, ApplicationUserId = alice.Id, Title = "Unique Alice Thread", Content = "Original thread body" };
            db.DiscussionThreads.AddRange(thread,
                new DiscussionThread { ForumId = forum.Id, ApplicationUserId = bob.Id, Title = "Unique Bob Thread", Content = "Bob thread body" },
                new DiscussionThread { ForumId = forum.Id, ApplicationUserId = legacy.Id, Title = "Unique Legacy Thread", Content = "Legacy thread body" });
            await db.SaveChangesAsync();
            var parent = new Post { DiscussionThreadId = thread.Id, ApplicationUserId = alice.Id, Title = "Parent", Content = "Unique parent contribution" };
            db.Posts.Add(parent);
            await db.SaveChangesAsync();
            var reply = new Post { DiscussionThreadId = thread.Id, ParentPostId = parent.Id, ApplicationUserId = bob.Id, Title = "Reply", Content = "Unique child contribution" };
            var legacyPost = new Post { DiscussionThreadId = thread.Id, ApplicationUserId = legacy.Id, Title = "Legacy", Content = "Unique legacy contribution" };
            db.Posts.AddRange(reply, legacyPost);
            await db.SaveChangesAsync();
            var client = factory.AuthenticatedClient(alice.Id);
            return new Fixture(factory, client, alice.Id)
            { ForumId = forum.Id, ThreadId = thread.Id, ParentPostId = parent.Id, ReplyPostId = reply.Id, LegacyPostId = legacyPost.Id };
        }

        public async Task<IDocument> GetAsync(string path)
        {
            using var response = await _client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await new HtmlParser().ParseDocumentAsync(await response.Content.ReadAsStringAsync());
        }

        public async Task ChangeAliceAsync(string name, string avatar)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var alice = await db.Users.SingleAsync(u => u.Id == _aliceId);
            alice.DisplayName = name;
            alice.AvatarId = avatar;
            await db.SaveChangesAsync();
        }

        public void Dispose() { _client.Dispose(); _factory.Dispose(); }
    }
}
