using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiscussionBoard.Tests.Authentication;

/// <summary>
/// Exercises the profile through real HTTP requests, without referencing a
/// particular PageModel, handler name, or validation-message wording.
/// </summary>
public sealed class MyProfileWorkflowTests
{
    private const string Email = "profile-workflow@example.test";
    private const string OriginalName = "Original Explorer";
    private const string OriginalAvatar = "fox";
    private const string ProfileUrl = "/MyProfile";

    [Fact(DisplayName = "MyProfile - valid changes are trimmed and persisted")]
    public async Task ValidUpdatePersistsBothFields()
    {
        using var session = await Session.CreateAsync();
        using var response = await session.SubmitAsync("  Updated Explorer  ", "astronaut");
        Assert.True(response.IsSuccessStatusCode || IsRedirect(response),
            $"Valid update returned {(int)response.StatusCode}.");
        await session.AssertStoredAsync("Updated Explorer", "astronaut");
    }

    [Theory(DisplayName = "MyProfile - invalid values reject the entire update")]
    [InlineData("x", "astronaut")]
    [InlineData("", "astronaut")]
    [InlineData("  ", "astronaut")]
    [InlineData("Updated Explorer", "not-in-catalog")]
    [InlineData("Updated Explorer", "")]
    public async Task InvalidUpdateIsAtomic(string displayName, string avatarId)
    {
        using var session = await Session.CreateAsync();
        using var response = await session.SubmitAsync(displayName, avatarId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(IsRedirect(response));
        await session.AssertStoredAsync(OriginalName, OriginalAvatar);
        await AssertFormRedisplayedAsync(response);
    }

    [Fact(DisplayName = "MyProfile - names longer than 60 characters are rejected")]
    public async Task LongNameIsRejected()
    {
        using var session = await Session.CreateAsync();
        using var response = await session.SubmitAsync(new string('X', 61), "astronaut");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await session.AssertStoredAsync(OriginalName, OriginalAvatar);
        await AssertFormRedisplayedAsync(response);
    }

    [Theory(DisplayName = "MyProfile - display-name uniqueness ignores case and whitespace")]
    [InlineData("ALICE")]
    [InlineData(" alice ")]
    public async Task DuplicateNameIsRejected(string name)
    {
        using var session = await Session.CreateAsync();
        await session.SeedOtherUserAsync("Alice");
        using var response = await session.SubmitAsync(name, "astronaut");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await session.AssertStoredAsync(OriginalName, OriginalAvatar);
        await AssertFormRedisplayedAsync(response);
    }

    [Fact(DisplayName = "MyProfile - saving your existing display name is permitted")]
    public async Task ExistingOwnNameCanBeSaved()
    {
        using var session = await Session.CreateAsync();
        using var response = await session.SubmitAsync("  Original Explorer  ", "robot");
        Assert.True(response.IsSuccessStatusCode || IsRedirect(response));
        await session.AssertStoredAsync(OriginalName, "robot");
    }

    [Fact(DisplayName = "MyProfile - anonymous visitors cannot read or update a profile")]
    public async Task AnonymousAccessIsDenied()
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.Client();
        using var get = await client.GetAsync(ProfileUrl);
        Assert.True(IsDenied(get),
            $"Anonymous GET returned {(int)get.StatusCode}.");

        using var post = await client.PostAsync(ProfileUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.DisplayName"] = "Hacked Name",
                ["Input.AvatarId"] = "robot"
            }));
        Assert.True(IsDenied(post),
            $"Anonymous POST returned {(int)post.StatusCode}.");
    }

    private static bool IsRedirect(HttpResponseMessage response) =>
        (int)response.StatusCode is >= 300 and < 400;

    // HTTP 400 is an expected antiforgery rejection for an anonymous POST without a token.
    private static bool IsDenied(HttpResponseMessage response) =>
        response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
        || (IsRedirect(response) &&
            (response.Headers.Location?.ToString().Contains("Login",
                StringComparison.OrdinalIgnoreCase) == true
             || response.Headers.Location?.ToString().Contains("AccessDenied",
                StringComparison.OrdinalIgnoreCase) == true));

    private static async Task AssertFormRedisplayedAsync(HttpResponseMessage response)
    {
        var doc = await new HtmlParser().ParseDocumentAsync(
            await response.Content.ReadAsStringAsync());
        Assert.True(doc.QuerySelectorAll("form[method='post']")
                .Any(form => form.QuerySelector("[name='Input.DisplayName']") is not null &&
                             form.QuerySelector("[name='Input.AvatarId']") is not null),
            "Rejected update should redisplay an editable profile form.");
    }

    private static string Field(IElement form, string property)
    {
        var names = form.QuerySelectorAll("input[name],select[name],textarea[name]")
            .Select(x => x.GetAttribute("name"))
            .Where(x => x is not null &&
                (x.Equals(property, StringComparison.OrdinalIgnoreCase) ||
                 x.EndsWith("." + property, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return Assert.Single(names)!;
    }

    private sealed class Session : IDisposable
    {
        private readonly AuthenticationWebTestFactory _factory;
        private readonly HttpClient _client;

        private Session(AuthenticationWebTestFactory factory, HttpClient client)
        {
            _factory = factory;
            _client = client;
        }

        public static async Task<Session> CreateAsync()
        {
            var factory = new AuthenticationWebTestFactory();
            try
            {
                await factory.InitializeDatabaseAsync();
                var client = factory.Client();
                var session = new Session(factory, client);
                await session.RegisterAndSignInAsync();
                return session;
            }
            catch
            {
                factory.Dispose();
                throw;
            }
        }

        private async Task RegisterAndSignInAsync()
        {
            using var get = await _client.GetAsync("/Identity/Account/Register");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var doc = await new HtmlParser().ParseDocumentAsync(
                await get.Content.ReadAsStringAsync());
            var form = doc.QuerySelector("form#registerForm")
                       ?? doc.QuerySelector("form[method='post']");
            Assert.True(form is not null, "Registration form was not found.");
            string token = form.QuerySelector("input[name='__RequestVerificationToken']")
                ?.GetAttribute("value") ?? "";
            Assert.False(string.IsNullOrWhiteSpace(token));

            var fields = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                [Field(form, "Email")] = Email,
                [Field(form, "Password")] = "ExamplePass123!",
                [Field(form, "ConfirmPassword")] = "ExamplePass123!",
                [Field(form, "DisplayName")] = OriginalName,
                [Field(form, "AvatarId")] = OriginalAvatar
            };
            var action = form.GetAttribute("action");
            if (string.IsNullOrWhiteSpace(action)) action = "/Identity/Account/Register";
            using var post = await _client.PostAsync(action,
                new FormUrlEncodedContent(fields));
            Assert.True(IsRedirect(post),
                $"Test setup registration did not redirect: {(int)post.StatusCode}.");
            await AssertStoredAsync(OriginalName, OriginalAvatar);
            // The test client retains the registration authentication cookie.
            using var profile = await _client.GetAsync(ProfileUrl);
            Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        }

        public async Task<HttpResponseMessage> SubmitAsync(string displayName, string avatarId)
        {
            using var get = await _client.GetAsync(ProfileUrl);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var doc = await new HtmlParser().ParseDocumentAsync(
                await get.Content.ReadAsStringAsync());
            var form = doc.QuerySelectorAll("form[method='post']")
                .FirstOrDefault(candidate =>
                    candidate.QuerySelectorAll("input[name],select[name],textarea[name]")
                        .Any(control => string.Equals(control.GetAttribute("name"),
                            "Input.DisplayName", StringComparison.OrdinalIgnoreCase)) &&
                    candidate.QuerySelectorAll("input[name],select[name],textarea[name]")
                        .Any(control => string.Equals(control.GetAttribute("name"),
                            "Input.AvatarId", StringComparison.OrdinalIgnoreCase)));
            Assert.True(form is not null,
                "No POST form containing both profile fields was found.");
            var token = form.QuerySelector("input[name='__RequestVerificationToken']")
                ?.GetAttribute("value") ?? "";
            Assert.False(string.IsNullOrWhiteSpace(token),
                "Profile form must have an antiforgery token.");
            var fields = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Input.DisplayName"] = displayName,
                ["Input.AvatarId"] = avatarId
            };
            var action = form.GetAttribute("action");
            if (string.IsNullOrWhiteSpace(action)) action = ProfileUrl;
            var target = new Uri(get.RequestMessage!.RequestUri!, action);
            Assert.Equal(ProfileUrl, target.AbsolutePath);
            return await _client.PostAsync(target, new FormUrlEncodedContent(fields));
        }

        public async Task AssertStoredAsync(string name, string avatar)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.AsNoTracking().SingleAsync(x => x.Email == Email);
            Assert.Equal(name, user.DisplayName);
            Assert.Equal(avatar, user.AvatarId);
        }

        public async Task SeedOtherUserAsync(string name)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.Add(new ApplicationUser
            {
                Id = "other-profile-user",
                UserName = "other-profile@example.test",
                NormalizedUserName = "OTHER-PROFILE@EXAMPLE.TEST",
                Email = "other-profile@example.test",
                NormalizedEmail = "OTHER-PROFILE@EXAMPLE.TEST",
                DisplayName = name,
                AvatarId = "default"
            });
            await db.SaveChangesAsync();
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
