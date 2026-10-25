using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Pages.Discussions;

[Authorize]
public class Edit : PageModel
{
    private readonly DiscussionThreadService _threadService;

    public Edit(DiscussionThreadService threadService)
    {
        _threadService = threadService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(10000)]
        public string Content { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var thread = await _threadService.GetAsync(id);

        if (thread == null)
            return NotFound();

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (thread.ApplicationUserId != userId)
            return Forbid();

        Input = new InputModel
        {
            Title = thread.Title,
            Content = thread.Content
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
            return Page();

        var thread = await _threadService.GetAsync(id);

        if (thread == null)
            return NotFound();

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (thread.ApplicationUserId != userId)
            return Forbid();

        // Only the permitted values onto the existing thread.
        thread.Title = Input.Title;
        thread.Content = Input.Content;

        try
        {
            await _threadService.UpdateAsync(thread);

            return RedirectToPage(
                "/Forums/Details",
                new { id = thread.ForumId });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The discussion could not be updated. Please try again.");

            return Page();
        }
    }
}
