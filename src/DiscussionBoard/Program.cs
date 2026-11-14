using System.Security.Cryptography;
using DiscussionBoard.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DiscussionBoard.Data;
using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using Serilog.Formatting.Compact;

namespace DiscussionBoard
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Get the location of the Logs folder for SeriLog
            string logPath = Path.Combine(builder.Environment.ContentRootPath, "Logs",  "discussionboard-.json");

            builder.Services.AddSerilog((services, configuration) => configuration
                .WriteTo.Console()
                .WriteTo.File(
                    new RenderedCompactJsonFormatter(),
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    shared: true)
            );

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                                   throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
                    options.SignIn.RequireConfirmedAccount = !builder.Environment.IsDevelopment())
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();


            //added because of docker maybe??
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SecurePolicy =
                    builder.Environment.IsEnvironment("Testing")
                        ? CookieSecurePolicy.SameAsRequest
                        : CookieSecurePolicy.Always;
            });

            builder.Services.Configure<AntiforgeryOptions>(options =>
            {
                options.Cookie.SecurePolicy =
                    builder.Environment.IsEnvironment("Testing")
                        ? CookieSecurePolicy.SameAsRequest
                        : CookieSecurePolicy.Always;
            });
            //end docker crap

            builder.Services.AddRazorPages(options =>
            {
                if (!builder.Environment.IsEnvironment("Testing"))
                {
                    options.Conventions.AuthorizeFolder("/Forums");
                    options.Conventions.AuthorizeFolder("/Discussions");
                    options.Conventions.AuthorizePage("/MyProfile");

                }
                options.Conventions.AddPageRoute(
                    "/Discussions/Create",
                    "/Thread/Create/{forumId:int}");

                options.Conventions.AddPageRoute("/Forums/Index", "/Forums");
            });

            builder.Services.AddScoped<IApplicationAuthorizationService, ApplicationAuthorizationService>();

            builder.Services.AddScoped<DiscussionThreadService>();

            builder.Services.AddScoped<ForumService>();

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    ApplicationAuthorizationPolicies.IsOwnerOrAdmin,
                    policy => policy.Requirements.Add(
                        new IsOwnerOrAdminRequirement()));
            });

            builder.Services.AddScoped< IAuthorizationHandler, IsOwnerOrAdminHandler>();

            builder.Services.AddSingleton<ILogReader, JsonLogReader>();

            builder.Services.AddScoped<PostService>();

            builder.Services.AddScoped<IUserProfileService, UserProfileService>();


            var app = builder.Build();

            //more docker

            app.Use(async (context, next) =>
            {
                var nonce = Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(16));

                context.Items["CspNonce"] = nonce;

                context.Response.Headers["Content-Security-Policy"] =
                    $"default-src 'self'; " +
                    $"script-src 'self' 'nonce-{nonce}'; " +
                    $"style-src 'self'; " +
                    $"img-src 'self' data:; " +
                    $"font-src 'self' data:; " +
                    $"connect-src 'self'; " +
                    $"object-src 'none'; " +
                    $"base-uri 'self'; " +
                    $"form-action 'self'; " +
                    $"frame-ancestors 'self'";

                await next();
            });

            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";

                    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";

                    return Task.CompletedTask;
                });

                await next();
            });


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                await DevelopmentSeedService.SeedAsync(app.Services);
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days.
                // You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            }

            //more docker stuff:
            if (!app.Environment.IsEnvironment("Testing"))
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapRazorPages()
                .WithStaticAssets();


            app.Run();
        }
    }
}
