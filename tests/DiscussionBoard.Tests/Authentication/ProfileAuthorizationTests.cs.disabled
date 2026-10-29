using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DiscussionBoard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiscussionBoard.Tests.Authentication;

/// <summary>HTTP-level authorization and overposting checks using two independently authenticated users.</summary>
public sealed class ProfileAuthorizationTests
{
    private const string ProfilePath = "/MyProfile";
    private const string Password = "ExamplePass123!";
    private const string AliceEmail = "profile-alice@example.test";
    private const string BobEmail = "profile-bob@example.test";

    [Theory(DisplayName = "MyProfile - forged user identifiers cannot modify another user's profile")]
    [InlineData("Input.UserId")]
    [InlineData("UserId")]
    [InlineData("Input.Id")]
    [InlineData("Id")]
    public async Task ForgedTargetIdCannotChangeAnotherUser(string targetField)
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        using var alice = factory.Client();
        using var bob = factory.Client();
        await RegisterAsync(alice, AliceEmail, "Alice Explorer", "fox");
        await RegisterAsync(bob, BobEmail, "Bob Explorer", "robot");
        var bobId = await UserIdAsync(factory, BobEmail);

        // Alice posts a valid edit to her own profile while forging Bob's ID.
        using var response = await SubmitProfileAsync(alice, "Alice Updated", "astronaut",
            new Dictionary<string, string> { [targetField] = bobId });
        // Either rejecting the suspicious request or updating Alice alone is secure.
        // The essential rule is that a forged identifier never changes Bob.
        Assert.True(response.IsSuccessStatusCode || IsRedirect(response) ||
                    response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden,
            $"Profile POST returned {(int)response.StatusCode}.");
        await AssertProfileAsync(factory, BobEmail, "Bob Explorer", "robot");
    }

    [Fact(DisplayName = "MyProfile - separate authenticated sessions edit only their own accounts")]
    public async Task EachAuthenticatedSessionEditsOnlyItsOwner()
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        using var alice = factory.Client();
        using var bob = factory.Client();
        await RegisterAsync(alice, AliceEmail, "Alice Explorer", "fox");
        await RegisterAsync(bob, BobEmail, "Bob Explorer", "robot");

        using (var response = await SubmitProfileAsync(alice, "Alice Updated", "astronaut"))
            Assert.True(response.IsSuccessStatusCode || IsRedirect(response));
        await AssertProfileAsync(factory, AliceEmail, "Alice Updated", "astronaut");
        await AssertProfileAsync(factory, BobEmail, "Bob Explorer", "robot");

        using (var response = await SubmitProfileAsync(bob, "Bob Updated", "fox"))
            Assert.True(response.IsSuccessStatusCode || IsRedirect(response));
        await AssertProfileAsync(factory, AliceEmail, "Alice Updated", "astronaut");
        await AssertProfileAsync(factory, BobEmail, "Bob Updated", "fox");
    }

    [Fact(DisplayName = "MyProfile - anonymous POST cannot change an existing user's profile")]
    public async Task AnonymousPostDoesNotModifyExistingProfile()
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        using var owner = factory.Client();
        await RegisterAsync(owner, AliceEmail, "Alice Explorer", "fox");
        using var anonymous = factory.Client();
        using var get = await anonymous.GetAsync(ProfilePath);
        Assert.True(IsDenied(get), $"Anonymous GET returned {(int)get.StatusCode}.");

        // A token-less request may be rejected by antiforgery before authorization.
        using var post = await anonymous.PostAsync(ProfilePath,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.DisplayName"] = "Attacker Updated",
                ["Input.AvatarId"] = "astronaut",
                ["UserId"] = await UserIdAsync(factory, AliceEmail)
            }));
        Assert.True(IsDenied(post), $"Anonymous POST returned {(int)post.StatusCode}.");
        await AssertProfileAsync(factory, AliceEmail, "Alice Explorer", "fox");
    }

    private static bool IsRedirect(HttpResponseMessage response) =>
        (int)response.StatusCode is >= 300 and < 400;

    private static bool IsDenied(HttpResponseMessage response) =>
        response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
        || (IsRedirect(response) &&
            (response.Headers.Location?.ToString().Contains("Login", StringComparison.OrdinalIgnoreCase) == true ||
             response.Headers.Location?.ToString().Contains("AccessDenied", StringComparison.OrdinalIgnoreCase) == true));

    private static async Task<IElement> FormAsync(HttpResponseMessage response, params string[] fields)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await new HtmlParser().ParseDocumentAsync(await response.Content.ReadAsStringAsync());
        var form = doc.QuerySelectorAll("form[method='post']").FirstOrDefault(candidate =>
            fields.All(field => candidate.QuerySelectorAll("input[name],select[name],textarea[name]")
                .Any(control => string.Equals(control.GetAttribute("name"), field,
                    StringComparison.OrdinalIgnoreCase) ||
                    control.GetAttribute("name")?.EndsWith("." + field,
                        StringComparison.OrdinalIgnoreCase) == true)));
        Assert.True(form is not null, "Could not locate the requested form by its submitted fields.");
        return form;
    }

    private static string Name(IElement form, string suffix)
    {
        var names = form.QuerySelectorAll("input[name],select[name],textarea[name]")
            .Select(x => x.GetAttribute("name"))
            .Where(x => x is not null &&
                (x.Equals(suffix, StringComparison.OrdinalIgnoreCase) ||
                 x.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return Assert.Single(names)!;
    }

    private static async Task RegisterAsync(HttpClient client, string email, string name, string avatar)
    {
        using var get = await client.GetAsync("/Identity/Account/Register");
        var form = await FormAsync(get, "Email", "Password", "ConfirmPassword", "DisplayName", "AvatarId");
        var token = form.QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value");
        Assert.False(string.IsNullOrWhiteSpace(token));
        var values = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token!,
            [Name(form, "Email")] = email,
            [Name(form, "Password")] = Password,
            [Name(form, "ConfirmPassword")] = Password,
            [Name(form, "DisplayName")] = name,
            [Name(form, "AvatarId")] = avatar
        };
        var action = form.GetAttribute("action");
        var target = new Uri(get.RequestMessage!.RequestUri!,
            string.IsNullOrWhiteSpace(action) ? "/Identity/Account/Register" : action);
        using var post = await client.PostAsync(target, new FormUrlEncodedContent(values));
        Assert.True(IsRedirect(post), $"Registration failed with HTTP {(int)post.StatusCode}.");
        using var profile = await client.GetAsync(ProfilePath);
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    private static async Task<HttpResponseMessage> SubmitProfileAsync(
        HttpClient client, string name, string avatar, Dictionary<string, string>? extra = null)
    {
        using var get = await client.GetAsync(ProfilePath);
        var form = await FormAsync(get, "DisplayName", "AvatarId");
        var token = form.QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value");
        Assert.False(string.IsNullOrWhiteSpace(token));
        var values = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token!,
            [Name(form, "DisplayName")] = name,
            [Name(form, "AvatarId")] = avatar
        };
        if (extra is not null)
            foreach (var pair in extra) values[pair.Key] = pair.Value;
        var action = form.GetAttribute("action");
        var target = new Uri(get.RequestMessage!.RequestUri!,
            string.IsNullOrWhiteSpace(action) ? ProfilePath : action);
        Assert.Equal(ProfilePath, target.AbsolutePath);
        return await client.PostAsync(target, new FormUrlEncodedContent(values));
    }

    private static async Task<string> UserIdAsync(AuthenticationWebTestFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Users.AsNoTracking().Where(x => x.Email == email).Select(x => x.Id).SingleAsync();
    }

    private static async Task AssertProfileAsync(AuthenticationWebTestFactory factory,
        string email, string name, string avatar)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Email == email);
        Assert.Equal(name, user.DisplayName);
        Assert.Equal(avatar, user.AvatarId);
    }
}
