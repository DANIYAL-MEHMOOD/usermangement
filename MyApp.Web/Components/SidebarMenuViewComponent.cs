using Microsoft.AspNetCore.Mvc;
using MyApp.Web.DTOs;
using MyApp.Web.Services.Interfaces;

namespace MyApp.Web.Components;

/// <summary>
/// Renders the sidebar dynamically from IMenuService.GetUserMenusAsync().
/// Every user sees only the branches their role (or an explicit per-user override) grants VIEW on.
/// </summary>
public class SidebarMenuViewComponent : ViewComponent
{
    private readonly IMenuService _menuService;

    public SidebarMenuViewComponent(IMenuService menuService)
    {
        _menuService = menuService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var response = await _menuService.GetUserMenusAsync();
        var menus = response is { Success: true, Data: not null } ? response.Data : [];
        return View(menus);
    }
}
