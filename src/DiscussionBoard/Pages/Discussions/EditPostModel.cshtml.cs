using DiscussionBoard.Authorization;
using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Pages.Discussions;

[Authorize]
public class EditPostModel : PageModel
{
    private readonly PostService _postService;
    private readonly IApplicationAuthorizationService _authorizationService;

    public EditPostModel(
        PostService postService,
        IApplicationAuthorizationService authorizationService)
    {
        _postService = postService;
        _authorizationService = authorizationService;
    }

    [BindProperty]
    public DiscussionEditViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int threadId, int postId)
    {
        var post = await _postService.GetAsync(postId);

        if (post == null || post.DiscussionThreadId != threadId)
            return NotFound();

        if (!await _authorizationService.CanEditAsync(User, post))
            return Forbid();

        Input = new DiscussionEditViewModel
        {
            Title = post.Title,
            Content = post.Content
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int threadId, int postId)
    {
        var post = await _postService.GetAsync(postId);

        if (post == null || post.DiscussionThreadId != threadId)
            return NotFound();

        if (!await _authorizationService.CanEditAsync(User, post))
            return Forbid();

        if (!ModelState.IsValid)
            return Page();

        post.Title = Input.Title;
        post.Content = Input.Content;

        try
        {
            await _postService.UpdateAsync(post);

            return RedirectToPage(
                "/Discussions/Details",
                new { threadId = threadId });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The post could not be updated. Please try again.");

            return Page();
        }
    }
}
