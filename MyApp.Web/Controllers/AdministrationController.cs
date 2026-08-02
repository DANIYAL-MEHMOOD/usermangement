using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Web.DTOs;
using MyApp.Web.Filters;
using MyApp.Web.Services.Interfaces;
using MyApp.Web.ViewModels;

namespace MyApp.Web.Controllers;

/// <summary>
/// Central administration hub with tabs for Users, Roles, Menus, Permissions
/// and Settings. Thin controller: only assembles the tab view-model from
/// service results.
/// </summary>
[Authorize]
[ServiceFilter(typeof(AdminOnlyFilter))]
public class AdministrationController : Controller
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly IMenuService _menuService;
    private readonly IProfileService _profileService;

    public AdministrationController(
        IUserService userService,
        IRoleService roleService,
        IMenuService menuService,
        IProfileService profileService)
    {
        _userService = userService;
        _roleService = roleService;
        _menuService = menuService;
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? tab = "users")
    {
        var model = new AdministrationViewModel
        {
            ActiveTab = string.IsNullOrWhiteSpace(tab) ? "users" : tab
        };

        // Users tab
        var usersResponse = await _userService.SearchAsync(new UserSearchRequestDto { PageSize = 5 });
        if (usersResponse is { Success: true, Data: not null })
        {
            model.RecentUsers = usersResponse.Data;
        }

        // Roles tab
        var rolesResponse = await _roleService.GetAllAsync(true);
        if (rolesResponse is { Success: true, Data: not null })
        {
            model.Roles = rolesResponse.Data;
        }

        // Menus tab
        var menusResponse = await _menuService.GetAllAsync();
        if (menusResponse is { Success: true, Data: not null })
        {
            model.Menus = menusResponse.Data;
        }

        // Settings tab
        var prefsResponse = await _profileService.GetPreferencesAsync();
        if (prefsResponse is { Success: true, Data: not null })
        {
            model.Theme = prefsResponse.Data.Theme;
            model.Language = prefsResponse.Data.Language;
            model.SidebarCollapsed = prefsResponse.Data.SidebarCollapsed;
        }

        return View(model);
    }
}
