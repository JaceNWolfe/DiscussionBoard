using DiscussionBoard.Authorization;
using DiscussionBoard.Pages;
using DiscussionBoard.Pages.Admin;
using Microsoft.AspNetCore.Authorization;

namespace DiscussionBoard.Tests.Authorization;

public sealed class PageAuthorizationContractTests
{
    [Theory]
    [InlineData(typeof(UsersModel))]
    [InlineData(typeof(Logs))]
    public void AdminPagesRequireAdminRole(Type pageType)
    {
        var attributes = pageType.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>();
        Assert.Contains(attributes, a => a.Roles?.Split(',').Select(x => x.Trim()).Contains(ApplicationRoles.Admin) == true);
    }

    [Theory]
    [InlineData(typeof(ThreadEditModel))]
    [InlineData(typeof(EditPostModel))]
    public void EditPagesRequireAuthentication(Type pageType)
    {
        Assert.NotEmpty(pageType.GetCustomAttributes(typeof(AuthorizeAttribute), true));
    }
}
