using DiscussionBoard.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly ApplicationDbContext _context;

        public UserProfileService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsDisplayNameAvailableAsync(
            string displayName,
            string? excludeUserId = null)
        {
            // Reject empty or whitespace-only names.
            if (string.IsNullOrWhiteSpace(displayName))
                return false;

            // Remove leading and trailing whitespace.
            displayName = displayName.Trim();

            // Enforce the 2-60 character requirement.
            if (displayName.Length < 2 || displayName.Length > 60)
                return false;

            // Look up existing display names.
            var users = _context.Users.AsQueryable();

            // Ignore the current user when an ID is provided.
            if (excludeUserId != null)
            {
                users = users.Where(u => u.Id != excludeUserId);
            }

            // Check for an existing name without regard to capitalization.
            bool exists = await users.AnyAsync(u =>
                EF.Functions.Collate(u.DisplayName, "NOCASE") == displayName);

            return !exists;
        }
    }
}
