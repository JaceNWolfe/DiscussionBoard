using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscussionBoard.Services
{
    public class PostService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PostService> _logger;

        public PostService(ApplicationDbContext context)
        {
            _context = context;
            _logger =
                Microsoft.Extensions.Logging.Abstractions
                    .NullLogger<PostService>.Instance;
        }

        public PostService(
            ApplicationDbContext context,
            ILogger<PostService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Post>> GetForThreadAsync(int threadId)
        {
            return await _context.Posts
                .AsNoTracking()
                .Include(p => p.ApplicationUser)
                .Where(p =>
                    p.DiscussionThreadId == threadId &&
                    !p.IsDeleted)
                .ToListAsync();
        }

        public async Task AddAsync(Post post)
        {
            try
            {
                await _context.Posts.AddAsync(post);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to save post for thread {ThreadId}.",
                    post.DiscussionThreadId);

                throw;
            }
        }

        public async Task<Post?> GetAsync(int id)
        {
            return await _context.Posts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        public async Task UpdateAsync(Post post)
        {
            var existingPost = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == post.Id && !p.IsDeleted);

            if (existingPost == null)
                throw new KeyNotFoundException("Post not found.");

            existingPost.Title = post.Title;
            existingPost.Content = post.Content;

            await _context.SaveChangesAsync();
        }
    }
}
