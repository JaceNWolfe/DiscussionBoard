using DiscussionBoard.Data;
using DiscussionBoard.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Services
{
    public class ForumService
    {
        private readonly ApplicationDbContext _context;

        public ForumService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Forum>> GetAllAsync()
        {
            return await _context.Forums
                .Where(f => !f.IsDeleted)
                .ToListAsync();
        }
    }
}
