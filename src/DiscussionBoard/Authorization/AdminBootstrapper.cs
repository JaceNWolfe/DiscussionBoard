using DiscussionBoard.Models;
using Microsoft.AspNetCore.Identity;

namespace DiscussionBoard.Authorization
{
    /// <summary>
    /// Explicit, one-time administrator bootstrap. Invoke only via a trusted
    /// maintenance command, never automatically on every web-app startup.
    /// </summary>
    public static class AdminBootstrapper
    {
        public static async Task BootstrapAsync(IServiceProvider services, string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("BootstrapAdmin:Email must be configured.");
            }

            await using var scope = services.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var user = await users.FindByEmailAsync(email.Trim());

            if (user is null)
            {
                throw new InvalidOperationException("Bootstrap user does not exist. Register the account first.");
            }

            if (!await users.IsEmailConfirmedAsync(user))
            {
                throw new InvalidOperationException("Bootstrap account email must be confirmed.");
            }

            if (!await roles.RoleExistsAsync(ApplicationRoles.Admin))
            {
                var create = await roles.CreateAsync(new IdentityRole(ApplicationRoles.Admin));
                EnsureSuccess(create, "create Admin role");
            }

            if (!await users.IsInRoleAsync(user, ApplicationRoles.Admin))
            {
                var add = await users.AddToRoleAsync(user, ApplicationRoles.Admin);
                EnsureSuccess(add, "assign Admin role");
            }
        }

        private static void EnsureSuccess(IdentityResult result, string operation)
        {
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not {operation}: " +
                    string.Join("; ", result.Errors.Select(e => e.Description))
                );
            }
        }
    }
}
