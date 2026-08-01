using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Web.DTOs;
using MyApp.Web.Services.Interfaces;
using MyApp.Web.ViewModels;

namespace MyApp.Web.Controllers;

[Authorize]
[ServiceFilter(typeof(MyApp.Web.Filters.AdminOnlyFilter))]
public class PermissionsController : Controller
{
    private readonly IPermissionService _permissionService;
    private readonly IRoleService _roleService;

    public PermissionsController(IPermissionService permissionService, IRoleService roleService)
    {
        _permissionService = permissionService;
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? roleId = null)
    {
        var rolesResponse = await _roleService.GetAllAsync(true);
        var roles = rolesResponse is { Success: true, Data: not null } ? rolesResponse.Data : [];

        var targetRoleId = roleId ?? (roles.FirstOrDefault()?.RoleId ?? 0);
        var targetRoleName = roles.FirstOrDefault(r => r.RoleId == targetRoleId)?.RoleName ?? "";

        var typesResponse = await _permissionService.GetTypesAsync();
        var matrixResponse = targetRoleId > 0 ? await _permissionService.GetMatrixAsync(targetRoleId) : null;

        var model = new PermissionMatrixViewModel
        {
            Roles = roles,
            SelectedRoleId = targetRoleId,
            SelectedRoleName = targetRoleName,
            PermissionTypes = typesResponse is { Success: true, Data: not null } ? typesResponse.Data : [],
            Matrix = matrixResponse is { Success: true, Data: not null } ? matrixResponse.Data : []
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int roleId, [FromBody] List<PermissionCellDto> permissions)
    {
        var dto = new AssignPermissionsRequestDto
        {
            RoleId = roleId,
            Permissions = permissions
        };

        var response = await _permissionService.AssignAsync(dto);
        if (response is { Success: true })
        {
            return Json(new { success = true, message = "Permissions saved successfully." });
        }

        return Json(new { success = false, message = response?.Message ?? "Failed to assign permissions." });
    }
}
