using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscussionBoard.Services
{
    public class DiscussionThreadService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DiscussionThreadService> _logger;

        public DiscussionThreadService(ApplicationDbContext context)
        {
            _context = context;
            _logger =
                Microsoft.Extensions.Logging.Abstractions
                    .NullLogger<DiscussionThreadService>.Instance;
        }

        public DiscussionThreadService(
            ApplicationDbContext context,
            ILogger<DiscussionThreadService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DiscussionThread?> GetAsync(int id)
        {
            return await _context.DiscussionThreads
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }

        public async Task<List<DiscussionThread>> GetForForumAsync(int forumId)
        {
            return await _context.DiscussionThreads
                .AsNoTracking()
                .Where(t => t.ForumId == forumId && !t.IsDeleted)
                .ToListAsync();
        }

        public async Task AddAsync(DiscussionThread thread)
        {
            try
            {
                await _context.DiscussionThreads.AddAsync(thread);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to add discussion thread.");
                throw;
            }
        }

        public async Task UpdateAsync(DiscussionThread thread)
        {
            try
            {
                var existingThread = await _context.DiscussionThreads
                    .FirstOrDefaultAsync(t =>
                        t.Id == thread.Id && !t.IsDeleted);

                if (existingThread == null)
                {
                    throw new KeyNotFoundException(
                        $"Discussion thread {thread.Id} was not found.");
                }

                existingThread.Title = thread.Title;
                existingThread.Content = thread.Content;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update discussion thread {ThreadId}.",
                    thread.Id);
                throw;
            }
        }
    }
}
