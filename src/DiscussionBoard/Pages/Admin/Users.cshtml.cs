using DiscussionBoard.Authorization;
using DiscussionBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Pages.Admin;

[Authorize(Roles = ApplicationRoles.Admin)]
public class UsersModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationAuthorizationService _authorizationService;

    public UsersModel(
        UserManager<ApplicationUser> userManager,
        IApplicationAuthorizationService authorizationService)
    {
        _userManager = userManager;
        _authorizationService = authorizationService;
    }

    [BindProperty(SupportsGet = true)]
    public string? SelectedUserId { get; set; }

    [BindProperty]
    public string? RoleName { get; set; }

    [BindProperty]
    public string? RoleAction { get; set; }

    public List<SelectListItem> UserOptions { get; set; } = new();

    public ApplicationUser? SelectedUser { get; set; }

    public bool IsAdmin { get; set; }

    public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        await LoadUsersAsync();

        if (!string.IsNullOrWhiteSpace(SelectedUserId))
            await ReloadSelectedUserAsync();
    }

    public async Task<IActionResult> OnPostSelectUserAsync()
    {
        await LoadUsersAsync();

        if (string.IsNullOrWhiteSpace(SelectedUserId))
        {
            Message = "Please select a user.";
            return Page();
        }

        SelectedUser = await _userManager.FindByIdAsync(SelectedUserId);

        if (SelectedUser == null)
        {
            Message = "The selected user could not be found.";
            SelectedUserId = null;
            return Page();
        }

        IsAdmin = await _userManager.IsInRoleAsync(
            SelectedUser, ApplicationRoles.Admin);

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateRoleAsync()
    {
        return await UpdateRoleAsync();
    }

    private async Task<IActionResult> UpdateRoleAsync()
    {
        await LoadUsersAsync();

        if (string.IsNullOrWhiteSpace(SelectedUserId))
        {
            Message = "A valid user must be selected.";
            return RedirectToPage(new { selectedUserId = SelectedUserId });
        }

        if (RoleName != ApplicationRoles.Admin)
        {
            Message = "The submitted role is not supported.";
            return RedirectToPage(new { selectedUserId = SelectedUserId });
        }

        if (RoleAction != "Add" && RoleAction != "Remove")
        {
            Message = "The submitted role action is invalid.";
            return RedirectToPage(new { selectedUserId = SelectedUserId });
        }

        var result = RoleAction == "Add"
            ? await _authorizationService.AddRoleAsync(
                User, SelectedUserId, RoleName)
            : await _authorizationService.RemoveRoleAsync(
                User, SelectedUserId, RoleName);

        if (result.Succeeded)
        {
            Message = RoleAction == "Add"
                ? "Successfully added the Admin role."
                : "Successfully removed the Admin role.";
        }
        else
        {
            Message = string.Join(" ", result.Errors.Select(e => e.Description));
        }

        return RedirectToPage(new { selectedUserId = SelectedUserId });
    }

    private async Task LoadUsersAsync()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.DisplayName)
            .ToListAsync();

        UserOptions = users.Select(user => new SelectListItem
        {
            Value = user.Id,
            Text = $"{user.DisplayName} ({user.Email})",
            Selected = user.Id == SelectedUserId
        }).ToList();
    }

    private async Task ReloadSelectedUserAsync()
    {
        SelectedUser = await _userManager.FindByIdAsync(SelectedUserId!);

        if (SelectedUser != null)
        {
            IsAdmin = await _userManager.IsInRoleAsync(
                SelectedUser, ApplicationRoles.Admin);
        }
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        RoleName = ApplicationRoles.Admin;
        RoleAction = "Add";

        return await UpdateRoleAsync();
    }

    public async Task<IActionResult> OnPostRemoveAsync()
    {
        RoleName = ApplicationRoles.Admin;
        RoleAction = "Remove";

        return await UpdateRoleAsync();
    }
}
