using DiscussionBoard.Authorization;
using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Pages.Discussions;

[Authorize]
public class ThreadEditModel : PageModel
{
    public int ForumId { get; private set; }
    private readonly DiscussionThreadService _threadService;
    private readonly IApplicationAuthorizationService _authorizationService;

    public ThreadEditModel(
        DiscussionThreadService threadService,
        IApplicationAuthorizationService authorizationService)
    {
        _threadService = threadService;
        _authorizationService = authorizationService;
    }

    [BindProperty]
    public DiscussionEditViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var thread = await _threadService.GetAsync(id);

        if (thread == null)
            return NotFound();

        if (!await _authorizationService.CanEditAsync(User, thread))
            return Forbid();

        ForumId = thread.ForumId;

        Input = new DiscussionEditViewModel
        {
            Title = thread.Title,
            Content = thread.Content
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var thread = await _threadService.GetAsync(id);

        if (thread == null)
            return NotFound();

        if (!await _authorizationService.CanEditAsync(User, thread))
            return Forbid();

        ForumId = thread.ForumId;

        if (!ModelState.IsValid)
            return Page();

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
