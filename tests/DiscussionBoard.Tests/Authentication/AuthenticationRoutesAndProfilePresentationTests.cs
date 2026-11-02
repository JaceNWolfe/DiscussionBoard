using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiscussionBoard.Tests.Authentication;

/// <summary>HTTP behavior and accessible profile-form requirements from Parts B, C and D.</summary>
public sealed class AuthenticationRoutesAndProfilePresentationTests
{
    [Theory(DisplayName = "Public pages remain accessible without signing in")]
    [InlineData("/")]
    [InlineData("/Privacy")]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Account/Register")]
    public async Task PublicPagesRemainAccessible(string path)
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.Client();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory(DisplayName = "Protected pages challenge anonymous visitors and preserve destination")]
    [InlineData("/MyProfile")]
    public async Task ProtectedPagesChallengeAnonymousVisitors(string path)
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.Client();
        using var response = await client.GetAsync(path);
        AssertLoginChallenge(response, path);
    }

    [Fact(DisplayName = "Existing forum and discussion detail pages require login")]
    public async Task DetailPagesRequireAuthentication()
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        int forumId, threadId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "route-test@example.test", Email = "route-test@example.test", DisplayName = "Route Tester", AvatarId = "fox" };
            db.Users.Add(user);
            var forum = new Forum { Name = "Route access test", Description = "Access control" };
            db.Forums.Add(forum);
            await db.SaveChangesAsync();
            forumId = forum.Id;
            var thread = new DiscussionThread { ForumId = forumId, ApplicationUserId = user.Id, Title = "Protected discussion", Content = "Content" };
            db.DiscussionThreads.Add(thread);
            await db.SaveChangesAsync();
            threadId = thread.Id;
        }
        using var client = factory.Client();
        using var forumResponse = await client.GetAsync($"/Forum/{forumId}");
        AssertLoginChallenge(forumResponse, $"/Forum/{forumId}");
        using var threadResponse = await client.GetAsync($"/Discussion/{threadId}");
        AssertLoginChallenge(threadResponse, $"/Discussion/{threadId}");
    }

    [Fact(DisplayName = "MyProfile displays current values and labeled avatar previews")]
    public async Task ProfileFormShowsValuesAndAccessibleChoices()
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.Client();
        await RegisterAsync(client);
        using var response = await client.GetAsync("/MyProfile");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await ParseAsync(response);
        var form = doc.QuerySelectorAll("form").FirstOrDefault(f =>
            Controls(f).Any(x => HasSuffix(x, "DisplayName")) &&
            Controls(f).Any(x => HasSuffix(x, "AvatarId")));
        Assert.NotNull(form);
        var nameControl = Controls(form!).Single(x => HasSuffix(x, "DisplayName"));
        Assert.Equal("Profile Explorer", nameControl.GetAttribute("value"));
        var avatars = Controls(form).Where(x => HasSuffix(x, "AvatarId")).ToArray();
        Assert.NotEmpty(avatars);
        Assert.Contains(avatars, x => x.GetAttribute("value") == "fox" && IsSelected(x));
        // Require a usable label for each selection control, without imposing a CSS framework.
        foreach (var avatar in avatars)
        {
            var id = avatar.GetAttribute("id");
            bool labeled = !string.IsNullOrWhiteSpace(avatar.GetAttribute("aria-label")) ||
                (!string.IsNullOrWhiteSpace(id) && doc.QuerySelectorAll("label").Any(l => l.GetAttribute("for") == id && !string.IsNullOrWhiteSpace(l.TextContent))) ||
                avatar.Ancestors().OfType<IElement>().Any(x => x.LocalName == "label" && !string.IsNullOrWhiteSpace(x.TextContent));
            Assert.True(labeled, $"Avatar control '{avatar.GetAttribute("name")}' requires an accessible label.");
        }
        Assert.True(form.QuerySelectorAll("img").Any(i =>
            (i.GetAttribute("src") ?? "").Contains("fox", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(i.GetAttribute("alt"))),
            "The selected/current avatar should have a visible image with meaningful alt text.");
        Assert.True(form.QuerySelectorAll("img").Count() >= 2,
            "Avatar choices should have image previews, not just an ID selector.");
    }

    private static IEnumerable<IElement> Controls(IElement form) => form.QuerySelectorAll("input[name],select[name],textarea[name]");
    private static bool HasSuffix(IElement e, string suffix) =>
        (e.GetAttribute("name") ?? "").Equals(suffix, StringComparison.OrdinalIgnoreCase) ||
        (e.GetAttribute("name") ?? "").EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase);
    private static bool IsSelected(IElement e) => e.HasAttribute("checked") || e.HasAttribute("selected") ||
        e.LocalName == "select" && e.QuerySelector("option:checked")?.GetAttribute("value") == "fox";

    private static void AssertLoginChallenge(HttpResponseMessage response, string destination)
    {
        Assert.True((int)response.StatusCode is >= 300 and < 400,
            $"Expected login redirect for {destination}, got {(int)response.StatusCode}.");
        var url = response.Headers.Location?.ToString() ?? "";
        Assert.Contains("Login", url, StringComparison.OrdinalIgnoreCase);
        var decoded = Uri.UnescapeDataString(url);
        Assert.Contains(destination, decoded, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<IDocument> ParseAsync(HttpResponseMessage response) =>
        await new HtmlParser().ParseDocumentAsync(await response.Content.ReadAsStringAsync());

    private static async Task RegisterAsync(HttpClient client)
    {
        using var get = await client.GetAsync("/Identity/Account/Register");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var doc = await ParseAsync(get);
        var form = doc.QuerySelectorAll("form").First(f =>
            Controls(f).Any(x => HasSuffix(x, "Email")) &&
            Controls(f).Any(x => HasSuffix(x, "DisplayName")));
        var values = new Dictionary<string, string>();
        foreach (var (suffix, value) in new[] {
            ("Email", "profile-form@example.test"), ("Password", "ExamplePass123!"),
            ("ConfirmPassword", "ExamplePass123!"), ("DisplayName", "Profile Explorer"), ("AvatarId", "fox") })
        {
            var control = Controls(form).First(x => HasSuffix(x, suffix));
            values[control.GetAttribute("name")!] = value;
        }
        var token = form.QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value");
        Assert.False(string.IsNullOrWhiteSpace(token));
        values["__RequestVerificationToken"] = token!;
        var action = form.GetAttribute("action");
        var target = new Uri(get.RequestMessage!.RequestUri!, string.IsNullOrEmpty(action) ? "/Identity/Account/Register" : action);
        using var post = await client.PostAsync(target, new FormUrlEncodedContent(values));
        Assert.True((int)post.StatusCode is >= 300 and < 400, $"Registration returned {(int)post.StatusCode}.");
    }
}
