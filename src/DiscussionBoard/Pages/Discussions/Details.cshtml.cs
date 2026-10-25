using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DiscussionBoard.Data;
using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Pages.Discussions;

public class Details : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly DiscussionThreadService _threadService;
    private readonly PostService _postService;

    public Details(
        ApplicationDbContext context,
        DiscussionThreadService threadService,
        PostService postService)
    {
        _context = context;
        _threadService = threadService;
        _postService = postService;
    }

    public DiscussionThread? Thread { get; set; }

    public List<PostTreeBuilder.Node> RootPosts { get; set; } = new();

    [BindProperty]
    public ReplyInput Input { get; set; } = new();

    public class ReplyInput
    {
        [Required]
        [MaxLength(10000)]
        public string Content { get; set; } = string.Empty;

        public int? ParentPostId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int threadId)
    {
        if (!await LoadDiscussionAsync(threadId))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostReplyAsync(int threadId)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Challenge();
        }

        if (!await LoadDiscussionAsync(threadId))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.ParentPostId.HasValue)
        {
            bool parentExists = await _context.Posts.AnyAsync(p =>
                p.Id == Input.ParentPostId.Value &&
                p.DiscussionThreadId == threadId &&
                !p.IsDeleted);

            if (!parentExists)
            {
                ModelState.AddModelError(
                    nameof(Input.ParentPostId),
                    "The selected parent post does not belong to this discussion.");

                return Page();
            }
        }

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var post = new Post
        {
            Title = "Reply",
            Content = Input.Content,
            DiscussionThreadId = threadId,
            ParentPostId = Input.ParentPostId,
            ApplicationUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await _postService.AddAsync(post);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The reply could not be saved. Please try again.");

            return Page();
        }

        return RedirectToPage(
            "/Discussions/Details",
            new { threadId });
    }

    private async Task<bool> LoadDiscussionAsync(int threadId)
    {
        Thread = await _threadService.GetAsync(threadId);

        if (Thread == null)
        {
            return false;
        }

        var posts = await _postService.GetForThreadAsync(threadId);

        RootPosts = PostTreeBuilder.Build(posts);

        return true;
    }
}
