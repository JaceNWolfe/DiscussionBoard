using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace DiscussionBoard.Pages;
public class ForumsModel : PageModel
{
    private readonly ForumService _forums;
    private readonly ILogger<ForumsModel> _logger;

    public List<Forum> Items { get; private set; } = [];

    /// <summary>
    /// The Forums page, displaying all active forums
    /// </summary>
    /// <param name="forums">Service to manage forums</param>
    /// <param name="logger">Logging provider</param>
    public ForumsModel(ForumService forums, ILogger<ForumsModel> logger)
    {
        _forums = forums;
        _logger = logger;
    }

    /// <summary>
    /// Returns a list of the active forums on the site
    /// </summary>
    public async Task OnGetAsync()
    {
        Items = await _forums.GetAllAsync();
    }
}
