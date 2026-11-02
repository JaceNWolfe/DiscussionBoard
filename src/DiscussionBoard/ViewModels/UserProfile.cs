using System.ComponentModel.DataAnnotations;

namespace DiscussionBoard.ViewModels
{
    public class UserProfile
    {
        [Required(ErrorMessage = "Display name is required.")]
        [StringLength(60, MinimumLength = 2,
            ErrorMessage = "Display name must be between 2 and 60 characters.")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select an avatar.")]
        public string AvatarId { get; set; } = "default";
    }
}
