using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Filters;
using MyApp.Api.Models;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUser;

    public PermissionsController(IPermissionService permissionService, ICurrentUserService currentUser)
    {
        _permissionService = permissionService;
        _currentUser = currentUser;
    }

    [HttpGet("types")]
    public async Task<ActionResult<ApiResponse<List<PermissionType>>>> GetTypes()
    {
        var response = await _permissionService.GetTypesAsync();
        return Ok(response);
    }

    [HttpGet("matrix/{roleId:int}")]
    public async Task<ActionResult<ApiResponse<List<RolePermissionRow>>>> GetMatrix(int roleId)
    {
        var response = await _permissionService.GetMatrixAsync(roleId);
        return Ok(response);
    }

    [HttpPost("assign")]
    [AuthorizePermission("Permissions", "EDIT")]
    public async Task<ActionResult<ApiResponse<object>>> Assign([FromBody] AssignPermissionsRequest request)
    {
        var response = await _permissionService.AssignAsync(request, _currentUser.UserId!.Value);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("check")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> Check([FromQuery] string module, [FromQuery] string action)
    {
        if (_currentUser.UserId is not int userId || _currentUser.RoleId is not int roleId)
        {
            return Ok(ApiResponse<bool>.Ok(false));
        }

        var response = await _permissionService.HasPermissionAsync(userId, roleId, module, action);
        return Ok(response);
    }
}
