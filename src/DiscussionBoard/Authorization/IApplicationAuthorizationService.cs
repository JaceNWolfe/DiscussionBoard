using System.Security.Claims;
using DiscussionBoard.Models;
using Microsoft.AspNetCore.Identity;

namespace DiscussionBoard.Authorization;

public interface IApplicationAuthorizationService
{
    Task<bool> CanEditAsync(ClaimsPrincipal principal, DiscussionBase resource);
    Task<IdentityResult> AddRoleAsync(ClaimsPrincipal actingUser, string targetUserId, string roleName);
    Task<IdentityResult> RemoveRoleAsync(ClaimsPrincipal actingUser, string targetUserId, string roleName);
}
