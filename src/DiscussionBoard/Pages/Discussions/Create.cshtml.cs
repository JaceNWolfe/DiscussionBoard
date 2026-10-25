using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DiscussionBoard.Data;
using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Pages.Discussions;

[Authorize]
public class Create : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly DiscussionThreadService _threadService;

    public Create(
        ApplicationDbContext context,
        DiscussionThreadService threadService)
    {
        _context = context;
        _threadService = threadService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int ForumId { get; set; }

    public class InputModel
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(10000)]
        public string Content { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        bool forumExists = await _context.Forums
            .AnyAsync(f => f.Id == ForumId && !f.IsDeleted);

        if (!forumExists)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var forum = await _context.Forums
            .FirstOrDefaultAsync(f => f.Id == ForumId && !f.IsDeleted);

        if (forum == null)
            return NotFound();

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Challenge();

        // Map permitted values
        var thread = new DiscussionThread
        {
            Title = Input.Title,
            Content = Input.Content,
            ForumId = forum.Id,
            ApplicationUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await _threadService.AddAsync(thread);
            return RedirectToPage("/Forums/Details", new { id = forum.Id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The discussion could not be saved. Please try again.");
            return Page();
        }
    }
}
