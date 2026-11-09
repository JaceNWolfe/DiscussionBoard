using Microsoft.AspNetCore.Authorization;

namespace DiscussionBoard.Authorization;

public class IsOwnerOrAdminRequirement : IAuthorizationRequirement
{
}
