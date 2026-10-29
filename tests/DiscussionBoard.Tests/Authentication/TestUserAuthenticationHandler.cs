using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscussionBoard.Tests.Authentication;

/// <summary>Only requests carrying the test header are authenticated.</summary>
public sealed class TestUserAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Scheme = "IntegrationTestUser";
    public const string UserHeader = "X-Integration-Test-User-Id";

    public TestUserAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out var userIds) ||
            string.IsNullOrWhiteSpace(userIds.ToString()))
            return Task.FromResult(AuthenticateResult.NoResult());

        var id = userIds.ToString();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim(ClaimTypes.Name, id)
        }, Scheme);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme)));
    }
}
