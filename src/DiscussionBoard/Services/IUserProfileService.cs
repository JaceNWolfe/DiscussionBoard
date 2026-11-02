namespace DiscussionBoard.Services
{
    public interface IUserProfileService
    {
        Task<bool> IsDisplayNameAvailableAsync(
            string displayName,
            string? excludeUserId = null);
    }
}
