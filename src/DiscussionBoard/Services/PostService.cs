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
    }
}
