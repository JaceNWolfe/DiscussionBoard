using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Services;

/// <summary>Instructor-provided development-only seed data; never run in the test host.</summary>
public static class DevelopmentSeedService
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        if (await db.Forums.AnyAsync()) return;

        UserManager<ApplicationUser> users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        const string email = "demo@discussionboard.local";
        ApplicationUser? demo = await users.FindByEmailAsync(email);
        if (demo is null)
        {
            demo = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            IdentityResult result = await users.CreateAsync(demo, "Demo-Discussion-123!");
            if (!result.Succeeded)
                throw new InvalidOperationException("Development seed user could not be created: " +
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        var forum = new Forum { Name = "General Discussion", Description = "Questions and discussion about our course" };
        var help = new Forum { Name = "Technical Help", Description = "Share troubleshooting questions and solutions" };
        db.Forums.AddRange(forum, help);
        await db.SaveChangesAsync();
        var thread = new DiscussionThread
        {
            ForumId = forum.Id, Title = "Welcome to DiscussionBoard",
            Content = "Introduce yourself and share something you're looking forward to learning.",
            ApplicationUserId = demo.Id
        };
        db.DiscussionThreads.Add(thread);
        await db.SaveChangesAsync();
        var welcome = new Post
        {
            DiscussionThreadId = thread.Id, ApplicationUserId = demo.Id,
            Title = "Welcome", Content = "You can reply to this discussion to get started."
        };
        var separate = new Post
        {
            DiscussionThreadId = thread.Id, ApplicationUserId = demo.Id,
            Title = "Getting started", Content = "What are you hoping to learn in this course?"
        };
        db.Posts.AddRange(welcome, separate);
        await db.SaveChangesAsync();

        var reply = new Post
        {
            DiscussionThreadId = thread.Id, ApplicationUserId = demo.Id,
            ParentPostId = welcome.Id, Title = "First reply",
            Content = "Replies appear beneath the post they respond to."
        };
        db.Posts.Add(reply);
        await db.SaveChangesAsync();

        db.Posts.Add(new Post
        {
            DiscussionThreadId = thread.Id, ApplicationUserId = demo.Id,
            ParentPostId = reply.Id, Title = "Nested reply",
            Content = "You can also reply to an existing reply."
        });
        await db.SaveChangesAsync();
    }
}
