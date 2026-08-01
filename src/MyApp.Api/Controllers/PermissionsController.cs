using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Common;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;

namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionRepository _permissions;
    private readonly ICurrentUserService _currentUser;

    public PermissionsController(IPermissionRepository permissions, ICurrentUserService currentUser)
    {
        _permissions = permissions;
        _currentUser = currentUser;
    }

    [HttpGet("types")]
    public async Task<ActionResult<ApiResponse<object>>> GetTypes()
    {
        var types = await _permissions.GetPermissionTypesAsync();
        return Ok(ApiResponse<object>.Ok(types));
    }

    /// <summary>The full menu x permission-type grid for one role — what the
    /// Permission Matrix screen renders as checkboxes.</summary>
    [HttpGet("matrix/{roleId:int}")]
    public async Task<ActionResult<ApiResponse<List<PermissionMatrixCellDto>>>> GetMatrix(int roleId)
    {
        var rows = await _permissions.GetRolePermissionsAsync(roleId);
        var dtos = rows.Select(r => new PermissionMatrixCellDto(r.MenuId, r.MenuName, r.ParentMenuId,
            r.PermissionTypeId, r.Code, r.Granted)).ToList();
        return Ok(ApiResponse<List<PermissionMatrixCellDto>>.Ok(dtos));
    }

    /// <summary>Saves the entire matrix for a role in one call (full replace).</summary>
    [HttpPost("assign")]
    public async Task<ActionResult<ApiResponse<object>>> Assign(AssignPermissionsRequest request)
    {
        var pairs = request.Permissions.Select(p => (p.MenuId, p.PermissionTypeId)).ToList();
        await _permissions.AssignPermissionsAsync(request.RoleId, pairs, _currentUser.UserId ?? 0);
        return Ok(ApiResponse<object>.Ok(new { }, "Permissions saved."));
    }
}
