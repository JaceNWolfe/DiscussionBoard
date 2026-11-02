using DiscussionBoard.Models;
using DiscussionBoard.Services;
using DiscussionBoard.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiscussionBoard.Pages
{
    [Authorize]
    public class MyProfile : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserProfileService _profileService;

        public MyProfile(
            UserManager<ApplicationUser> userManager,
            IUserProfileService profileService)
        {
            _userManager = userManager;
            _profileService = profileService;
        }

        [BindProperty]
        public UserProfile Input { get; set; } = new();

        public IReadOnlyList<AvatarCatalog.AvatarOption> AvatarOptions
            => AvatarCatalog.Options;

        public string CurrentAvatarPath { get; set; } = AvatarCatalog.ImagePath("default");

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            Input = new UserProfile
            {
                DisplayName = user.DisplayName,
                AvatarId = user.AvatarId
            };

            CurrentAvatarPath = AvatarCatalog.ImagePath(user.AvatarId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // Trim the submitted display name.
            Input.DisplayName = Input.DisplayName?.Trim() ?? string.Empty;

            // Revalidate the trimmed value.
            ModelState.Remove("Input.DisplayName");
            TryValidateModel(Input, nameof(Input));

            // Validate the avatar ID against the catalog.
            if (!AvatarCatalog.IsValid(Input.AvatarId))
            {
                ModelState.AddModelError(
                    "Input.AvatarId",
                    "Please select a valid avatar.");
            }

            // Check whether the display name is available.
            if (ModelState.IsValid)
            {
                bool available = await _profileService.IsDisplayNameAvailableAsync(
                    Input.DisplayName,
                    user.Id);

                if (!available)
                {
                    ModelState.AddModelError(
                        "Input.DisplayName",
                        "That display name is already taken.");
                }
            }

            if (!ModelState.IsValid)
            {
                CurrentAvatarPath = AvatarCatalog.ImagePath(Input.AvatarId);
                return Page();
            }

            // Update only the profile properties.
            user.DisplayName = Input.DisplayName;
            user.AvatarId = Input.AvatarId;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                CurrentAvatarPath = AvatarCatalog.ImagePath(Input.AvatarId);
                return Page();
            }

            StatusMessage = "Your profile has been updated successfully.";

            return RedirectToPage();
        }
    }
}
