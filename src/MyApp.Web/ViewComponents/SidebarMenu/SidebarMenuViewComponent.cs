using Microsoft.AspNetCore.Mvc;
using MyApp.Web.Services;

namespace MyApp.Web.ViewComponents;

public record MenuNode(int MenuId, int? ParentMenuId, string MenuName, string? MenuIcon,
    string? ControllerPage, string? Route, int MenuOrder, bool Status, List<MenuNode> Children);

/// <summary>Renders the sidebar entirely from GET /api/menus/mine — there is no
/// hardcoded menu anywhere in the Web project. Every user sees only the branches
/// their role (or an explicit per-user override) grants VIEW on.</summary>
public class SidebarMenuViewComponent : ViewComponent
{
    private readonly IApiClient _api;

    public SidebarMenuViewComponent(IApiClient api) => _api = api;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var result = await _api.GetAsync<List<MenuNode>>("api/menus/mine");
        var menus = result is { Success: true, Data: not null } ? result.Data : [];
        return View(menus);
    }
}
