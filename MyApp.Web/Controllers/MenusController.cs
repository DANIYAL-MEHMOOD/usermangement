using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Web.DTOs;
using MyApp.Web.Services.Interfaces;
using MyApp.Web.ViewModels;

namespace MyApp.Web.Controllers;

[Authorize]
[ServiceFilter(typeof(MyApp.Web.Filters.AdminOnlyFilter))]
public class MenusController : Controller
{
    private readonly IMenuService _menuService;

    public MenusController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var response = await _menuService.GetAllAsync();
        var model = new MenuListViewModel
        {
            Menus = response is { Success: true, Data: not null } ? response.Data : []
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? parentId = null)
    {
        var menusResponse = await _menuService.GetAllAsync();
        var model = new MenuFormViewModel
        {
            ParentMenuId = parentId,
            ParentOptions = menusResponse is { Success: true, Data: not null } ? menusResponse.Data : []
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuFormViewModel model)
    {
        var dto = new SaveMenuRequestDto
        {
            ParentMenuId = model.ParentMenuId,
            Title = model.Title,
            Icon = model.Icon,
            ControllerPage = model.ControllerPage,
            Route = model.Route,
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive
        };

        var response = await _menuService.SaveAsync(dto);
        if (response is { Success: true })
        {
            TempData["SuccessMessage"] = "Menu created successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", response?.Message ?? "Failed to save menu.");
        var menusResponse = await _menuService.GetAllAsync();
        model.ParentOptions = menusResponse is { Success: true, Data: not null } ? menusResponse.Data : [];
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var menusResponse = await _menuService.GetAllAsync();
        var allMenus = menusResponse is { Success: true, Data: not null } ? menusResponse.Data : [];
        var item = FindMenu(allMenus, id);
        if (item is null)
            return NotFound();

        var model = new MenuFormViewModel
        {
            MenuId = item.MenuId,
            ParentMenuId = item.ParentMenuId,
            Title = item.Title,
            Icon = item.Icon,
            ControllerPage = item.ControllerPage,
            Route = item.Route,
            DisplayOrder = item.DisplayOrder,
            IsActive = item.IsActive,
            ParentOptions = allMenus
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MenuFormViewModel model)
    {
        var dto = new SaveMenuRequestDto
        {
            MenuId = id,
            ParentMenuId = model.ParentMenuId,
            Title = model.Title,
            Icon = model.Icon,
            ControllerPage = model.ControllerPage,
            Route = model.Route,
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive
        };

        var response = await _menuService.SaveAsync(dto);
        if (response is { Success: true })
        {
            TempData["SuccessMessage"] = "Menu updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", response?.Message ?? "Failed to update menu.");
        var menusResponse = await _menuService.GetAllAsync();
        model.ParentOptions = menusResponse is { Success: true, Data: not null } ? menusResponse.Data : [];
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _menuService.DeleteAsync(id);
        if (response is { Success: true })
        {
            TempData["SuccessMessage"] = "Menu deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = response?.Message ?? "Failed to delete menu.";
        }
        return RedirectToAction(nameof(Index));
    }

    private static MenuNodeDto? FindMenu(List<MenuNodeDto> list, int id)
    {
        foreach (var m in list)
        {
            if (m.MenuId == id) return m;
            var found = FindMenu(m.Children, id);
            if (found is not null) return found;
        }
        return null;
    }
}
