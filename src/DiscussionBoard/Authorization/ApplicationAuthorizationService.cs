using System.Security.Claims;
using DiscussionBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace DiscussionBoard.Authorization;

public class ApplicationAuthorizationService : IApplicationAuthorizationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthorizationService _authorizationService;

    public ApplicationAuthorizationService(
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService)
    {
        _userManager = userManager;
        _authorizationService = authorizationService;
    }

    public async Task<bool> CanEditAsync(
        ClaimsPrincipal principal,
        DiscussionBase resource)
    {
        var result = await _authorizationService.AuthorizeAsync(
            principal,
            resource,
            ApplicationAuthorizationPolicies.IsOwnerOrAdmin);

        return result.Succeeded;
    }

    public async Task<IdentityResult> AddRoleAsync(
        ClaimsPrincipal actingUser,
        string targetUserId,
        string roleName)
    {
        if (!await IsAuthenticatedAdminAsync(actingUser))
            return Failure("Only authenticated administrators can manage roles.");

        if (roleName != ApplicationRoles.Admin)
            return Failure("Unsupported role.");

        var targetUser = await _userManager.FindByIdAsync(targetUserId);

        if (targetUser == null)
            return Failure("The selected user could not be found.");

        if (await _userManager.IsInRoleAsync(targetUser, roleName))
            return Failure("The user already has this role.");

        return await _userManager.AddToRoleAsync(targetUser, roleName);
    }

    public async Task<IdentityResult> RemoveRoleAsync(
        ClaimsPrincipal actingUser,
        string targetUserId,
        string roleName)
    {
        if (!await IsAuthenticatedAdminAsync(actingUser))
            return Failure("Only authenticated administrators can manage roles.");

        if (roleName != ApplicationRoles.Admin)
            return Failure("Unsupported role.");

        var actingUserId = actingUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if (actingUserId == targetUserId)
            return Failure("You cannot remove your own Admin role.");

        var targetUser = await _userManager.FindByIdAsync(targetUserId);

        if (targetUser == null)
            return Failure("The selected user could not be found.");

        if (!await _userManager.IsInRoleAsync(targetUser, roleName))
            return Failure("The user is not an administrator.");

        var admins = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Admin);

        if (admins.Count <= 1)
            return Failure("You cannot remove the last remaining administrator.");

        return await _userManager.RemoveFromRoleAsync(targetUser, roleName);
    }

    private async Task<bool> IsAuthenticatedAdminAsync(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return false;

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var actingUser = await _userManager.FindByIdAsync(userId);

        if (actingUser == null)
            return false;

        return await _userManager.IsInRoleAsync(
            actingUser,
            ApplicationRoles.Admin);
    }

    private static IdentityResult Failure(string message)
    {
        return IdentityResult.Failed(new IdentityError
        {
            Description = message
        });
    }
}
