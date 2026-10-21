using DiscussionBoard.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<DiscussionThread> DiscussionThreads { get; set; }
        public DbSet<Forum> Forums { get; set; }
        public DbSet<Post> Posts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Call the parent class first.
            base.OnModelCreating(builder);

            // Forum has many discussion threads.
            builder.Entity<DiscussionThread>()
                .HasOne(d => d.Forum)
                .WithMany(f => f.DiscussionThreads)
                .HasForeignKey(d => d.ForumId);

            // DiscussionThread has many posts.
            builder.Entity<Post>()
                .HasOne(p => p.DiscussionThread)
                .WithMany(d => d.Posts)
                .HasForeignKey(p => p.DiscussionThreadId);

            // Posts can have a parent post and multiple replies.
            builder.Entity<Post>()
                .HasOne(p => p.ParentPost)
                .WithMany(p => p.Replies)
                .HasForeignKey(p => p.ParentPostId)
                .IsRequired(false);

            // ApplicationUser has many discussion threads.
            builder.Entity<DiscussionThread>()
                .HasOne(d => d.ApplicationUser)
                .WithMany(u => u.DiscussionThreads)
                .HasForeignKey(d => d.ApplicationUserId);

            // ApplicationUser has many posts.
            builder.Entity<Post>()
                .HasOne(p => p.ApplicationUser)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.ApplicationUserId);
        }
    }
}
