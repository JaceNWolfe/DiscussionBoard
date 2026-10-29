using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using DiscussionBoard.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DiscussionBoard.Tests.Authentication;

internal sealed class AuthenticationWebTestFactory : WebApplicationFactory<DiscussionBoard.Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly bool _enableTestUser;

    public AuthenticationWebTestFactory(bool enableTestUser = false) => _enableTestUser = enableTestUser;

    public async Task InitializeDatabaseAsync()
    {
        await _connection.OpenAsync();
        _ = Server;
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Database.EnsureCreatedAsync();
    }

    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });

    public HttpClient AuthenticatedClient(string userId)
    {
        var client = Client();
        client.DefaultRequestHeaders.Add(TestUserAuthenticationHandler.UserHeader, userId);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development matches the existing application's no-confirmation test behavior.
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            if (_enableTestUser)
            {
                services.AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, TestUserAuthenticationHandler>(
                        TestUserAuthenticationHandler.Scheme, _ => { });
                services.PostConfigure<AuthenticationOptions>(options =>
                    options.DefaultAuthenticateScheme = TestUserAuthenticationHandler.Scheme);
            }
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
