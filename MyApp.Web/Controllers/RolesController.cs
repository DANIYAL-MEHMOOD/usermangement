using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Web.DTOs;
using MyApp.Web.Services.Interfaces;
using MyApp.Web.ViewModels;

namespace MyApp.Web.Controllers;

[Authorize]
[ServiceFilter(typeof(MyApp.Web.Filters.AdminOnlyFilter))]
public class RolesController : Controller
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var response = await _roleService.GetAllAsync(true);
        var model = new RoleListViewModel
        {
            Roles = response is { Success: true, Data: not null } ? response.Data : []
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new RoleFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var dto = new SaveRoleRequestDto
        {
            RoleName = model.RoleName,
            Description = model.Description,
            IsActive = model.IsActive
        };

        var response = await _roleService.SaveAsync(dto);
        if (response is { Success: true })
        {
            TempData["SuccessMessage"] = "Role created successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", response?.Message ?? "Failed to save role.");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var response = await _roleService.GetByIdAsync(id);
        if (response is null || !response.Success || response.Data is null)
            return NotFound();

        var r = response.Data;
        var model = new RoleFormViewModel
        {
            RoleId = r.RoleId,
            RoleName = r.RoleName,
            Description = r.Description,
            IsActive = r.IsActive
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoleFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var dto = new SaveRoleRequestDto
        {
            RoleId = id,
            RoleName = model.RoleName,
            Description = model.Description,
            IsActive = model.IsActive
        };

        var response = await _roleService.SaveAsync(dto);
        if (response is { Success: true })
        {
            TempData["SuccessMessage"] = "Role updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", response?.Message ?? "Failed to update role.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _roleService.DeleteAsync(id);
        if (response is { Success: true })
        {
            TempData["SuccessMessage"] = "Role deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = response?.Message ?? "Failed to delete role.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Clone(int id)
    {
        var response = await _roleService.GetAllAsync(true);
        var model = new RoleCloneViewModel
        {
            SourceRoleId = id,
            ExistingRoles = response is { Success: true, Data: not null } ? response.Data : []
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clone(RoleCloneViewModel model)
    {
        var dto = new CloneRoleRequestDto
        {
            SourceRoleId = model.SourceRoleId,
            NewRoleName = model.NewRoleName,
            Description = model.Description
        };

        var response = await _roleService.CloneAsync(dto);
        if (response is { Success: true })
        {
            TempData["SuccessMessage"] = "Role cloned successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", response?.Message ?? "Failed to clone role.");
        var roles = await _roleService.GetAllAsync(true);
        model.ExistingRoles = roles is { Success: true, Data: not null } ? roles.Data : [];
        return View(model);
    }
}
