using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiscussionBoard.Tests.Authentication
{
    public sealed class RegistrationWorkflowTests
    {
        [Fact(DisplayName = "Registration - valid profile values are trimmed and persisted")]
        public async Task RegistrationPersistsProfileFields()
        {
            using var factory = new AuthenticationWebTestFactory();
            await factory.InitializeDatabaseAsync();
            using var client = factory.Client();

            const string email = "new-registration@example.test";
            using var response = await SubmitRegistrationAsync(client, email,
                "  Space Explorer  ", "astronaut");

            await AssertCreatedAsync(factory, email, "Space Explorer", "astronaut", response);
        }

        [Theory(DisplayName = "Registration - invalid profile input never creates an account")]
        [InlineData("ALICE", "fox")]
        [InlineData(" alice ", "fox")]
        [InlineData("x", "fox")]
        [InlineData("", "fox")]
        [InlineData("Good Name", "not-in-catalog")]
        public async Task RegistrationRejectsInvalidProfile(string displayName, string avatarId)
        {
            using var factory = new AuthenticationWebTestFactory();
            await factory.InitializeDatabaseAsync();
            using var client = factory.Client();

            await SeedExistingUserAsync(factory);
            const string email = "invalid@example.test";
            using var response = await SubmitRegistrationAsync(client, email, displayName, avatarId);

            await AssertNotCreatedAsync(factory, email, response);
        }

        [Fact(DisplayName = "Registration - names longer than 60 characters are rejected")]
        public async Task RegistrationRejectsLongName()
        {
            using var factory = new AuthenticationWebTestFactory();
            await factory.InitializeDatabaseAsync();
            using var client = factory.Client();

            const string email = "long-name@example.test";
            using var response = await SubmitRegistrationAsync(client, email,
                new string('A', 61), "fox");
            await AssertNotCreatedAsync(factory, email, response);
        }

        private static async Task SeedExistingUserAsync(AuthenticationWebTestFactory factory)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.Add(new ApplicationUser
            {
                Id = "existing-user",
                UserName = "existing@example.test",
                NormalizedUserName = "EXISTING@EXAMPLE.TEST",
                Email = "existing@example.test",
                NormalizedEmail = "EXISTING@EXAMPLE.TEST",
                DisplayName = "Alice",
                AvatarId = "default"
            });
            await db.SaveChangesAsync();
        }

        private static async Task<HttpResponseMessage> SubmitRegistrationAsync(
            HttpClient client, string email, string displayName, string avatarId)
        {
            using var get = await client.GetAsync("/Identity/Account/Register");
            Assert.True(get.IsSuccessStatusCode,
                $"Registration GET returned {(int)get.StatusCode}.");

            var document = await new HtmlParser().ParseDocumentAsync(
                await get.Content.ReadAsStringAsync());

            var form = document.QuerySelector("form#registerForm")
                       ?? document.QuerySelector("form[method='post']");
            Assert.NotNull(form);

            string token = form.QuerySelector("input[name='__RequestVerificationToken']")
                ?.GetAttribute("value") ?? "";
            Assert.False(string.IsNullOrWhiteSpace(token),
                "Registration must retain its antiforgery token.");

            // Resolve the names from actual HTML so nested Input.Profile and
            // flattened Input fields are both supported.
            string displayField = FindFieldName(form, "DisplayName");
            string avatarField = FindFieldName(form, "AvatarId");
            string emailField = FindFieldName(form, "Email");
            string passwordField = FindFieldName(form, "Password");
            string confirmationField = FindFieldName(form, "ConfirmPassword");

            var values = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                [emailField] = email,
                [passwordField] = "ExamplePass123!",
                [confirmationField] = "ExamplePass123!",
                [displayField] = displayName,
                [avatarField] = avatarId
            };

            string action = form.GetAttribute("action") ?? "/Identity/Account/Register";
            if (string.IsNullOrWhiteSpace(action)) action = "/Identity/Account/Register";
            return await client.PostAsync(action, new FormUrlEncodedContent(values));
        }

        private static string FindFieldName(IElement form, string property)
        {
            var names = form.QuerySelectorAll("input[name], select[name], textarea[name]")
                .Select(element => element.GetAttribute("name"))
                .Where(name => name is not null &&
                               (name.Equals(property, StringComparison.OrdinalIgnoreCase) ||
                                name.EndsWith("." + property, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Assert.Single(names)!;
        }

        private static async Task AssertCreatedAsync(AuthenticationWebTestFactory factory,
            string email, string expectedName, string expectedAvatar, HttpResponseMessage response)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email);
            Assert.True(user is not null,
                $"Registration did not create '{email}'. HTTP {(int)response.StatusCode}. " +
                $"Response: {await ResponseExcerptAsync(response)}");
            Assert.True((int)response.StatusCode is >= 300 and < 400,
                $"Successful registration should redirect, got {(int)response.StatusCode}. " +
                $"Response: {await ResponseExcerptAsync(response)}");
            Assert.NotNull(response.Headers.Location);
            Assert.Equal(expectedName, user!.DisplayName);
            Assert.Equal(expectedAvatar, user.AvatarId);
        }

        private static async Task AssertNotCreatedAsync(AuthenticationWebTestFactory factory,
            string email, HttpResponseMessage response)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await db.Users.AsNoTracking().AnyAsync(u => u.Email == email),
                $"Invalid registration created '{email}'. HTTP {(int)response.StatusCode}.");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Null(response.Headers.Location);
            Assert.Equal("/Identity/Account/Register",
                response.RequestMessage?.RequestUri?.AbsolutePath);

            var document = await new HtmlParser().ParseDocumentAsync(
                await response.Content.ReadAsStringAsync());
            Assert.True(document.QuerySelector("form[method='post']") is not null,
                "Rejected registration should redisplay a form so the user can correct the input.");
        }

        private static async Task<string> ResponseExcerptAsync(HttpResponseMessage response)
        {
            var html = await response.Content.ReadAsStringAsync();
            var document = await new HtmlParser().ParseDocumentAsync(html);
            var errors = document.QuerySelectorAll(
                    ".field-validation-error, .validation-summary-errors, [role='alert']")
                .Select(e => e.TextContent.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var result = string.Join(" | ", errors);
            return string.IsNullOrEmpty(result) ? html[..Math.Min(html.Length, 500)] : result;
        }
    }
}
