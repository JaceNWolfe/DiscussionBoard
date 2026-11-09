using System.Net;
using DiscussionBoard.Data;
using DiscussionBoard.Models;
using DiscussionBoard.Services;
using DiscussionBoard.Tests.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiscussionBoard.Tests.Authorization;

/// <summary>Exercises actual edit routes and verifies the persistence boundary against forged fields.</summary>
public sealed class EditSecurityTests
{
    private static async Task<(int ThreadId, int PostId)> SeedAsync(AuthenticationWebTestFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var owner = new ApplicationUser { Id = "edit-owner", UserName = "edit-owner", Email = "edit-owner@example.test" };
        var other = new ApplicationUser { Id = "edit-other", UserName = "edit-other", Email = "edit-other@example.test" };
        db.Users.AddRange(owner, other);
        var forum = new Forum { Name = "Security test forum" };
        db.Forums.Add(forum);
        var thread = new DiscussionThread { Forum = forum, ApplicationUserId = owner.Id, Title = "Original thread", Content = "Original thread content" };
        db.DiscussionThreads.Add(thread);
        var post = new Post { DiscussionThread = thread, ApplicationUserId = owner.Id, Title = "Original post", Content = "Original post content" };
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        return (thread.Id, post.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EditGetPermitsOwnerAndAdminButRejectsNonowner(bool post)
    {
        using var factory = new AuthenticationWebTestFactory(enableTestUser: true);
        await factory.InitializeDatabaseAsync();
        var (threadId, postId) = await SeedAsync(factory);
        var path = post ? $"/Discussion/{threadId}/Post/Edit/{postId}" : $"/Thread/Edit/{threadId}";
        using var owner = factory.AuthenticatedClient("edit-owner");
        using var stranger = factory.AuthenticatedClient("edit-other");
        using var admin = factory.AuthenticatedClient("edit-other");
        admin.DefaultRequestHeaders.Add(TestUserAuthenticationHandler.RoleHeader, "Admin");
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(path)).StatusCode);
        var denied = await stranger.GetAsync(path);
        Assert.True(denied.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Redirect);
        if (denied.StatusCode == HttpStatusCode.Redirect)
            Assert.Contains("AccessDenied", denied.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WrongThreadPostAssociationAndMissingResourcesReturnNotFound()
    {
        using var factory = new AuthenticationWebTestFactory(enableTestUser: true);
        await factory.InitializeDatabaseAsync();
        var (threadId, postId) = await SeedAsync(factory);
        using var owner = factory.AuthenticatedClient("edit-owner");
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/Discussion/{threadId + 123}/Post/Edit/{postId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync("/Thread/Edit/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/Discussion/{threadId}/Post/Edit/999999")).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateServiceCopiesOnlyEditableFields(bool post)
    {
        using var factory = new AuthenticationWebTestFactory();
        await factory.InitializeDatabaseAsync();
        var (threadId, postId) = await SeedAsync(factory);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (post)
        {
            var original = await db.Posts.AsNoTracking().SingleAsync(x => x.Id == postId);
            var forged = new Post
            {
                Id = postId, Title = "Edited post", Content = "Edited content",
                ApplicationUserId = "edit-other", DiscussionThreadId = threadId + 999,
                ParentPostId = 999, IsDeleted = true, CreatedAt = original.CreatedAt.AddDays(1)
            };
            await scope.ServiceProvider.GetRequiredService<PostService>().UpdateAsync(forged);
            var saved = await db.Posts.AsNoTracking().SingleAsync(x => x.Id == postId);
            Assert.Equal("Edited post", saved.Title);
            Assert.Equal("Edited content", saved.Content);
            Assert.Equal(original.ApplicationUserId, saved.ApplicationUserId);
            Assert.Equal(original.DiscussionThreadId, saved.DiscussionThreadId);
            Assert.Equal(original.ParentPostId, saved.ParentPostId);
            Assert.Equal(original.CreatedAt, saved.CreatedAt);
            Assert.False(saved.IsDeleted);
        }
        else
        {
            var original = await db.DiscussionThreads.AsNoTracking().SingleAsync(x => x.Id == threadId);
            var forged = new DiscussionThread
            {
                Id = threadId, Title = "Edited thread", Content = "Edited content",
                ApplicationUserId = "edit-other", ForumId = original.ForumId + 999,
                IsDeleted = true, CreatedAt = original.CreatedAt.AddDays(1)
            };
            await scope.ServiceProvider.GetRequiredService<DiscussionThreadService>().UpdateAsync(forged);
            var saved = await db.DiscussionThreads.AsNoTracking().SingleAsync(x => x.Id == threadId);
            Assert.Equal("Edited thread", saved.Title);
            Assert.Equal("Edited content", saved.Content);
            Assert.Equal(original.ApplicationUserId, saved.ApplicationUserId);
            Assert.Equal(original.ForumId, saved.ForumId);
            Assert.Equal(original.CreatedAt, saved.CreatedAt);
            Assert.False(saved.IsDeleted);
        }
    }
}
