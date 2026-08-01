using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Models;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    public async Task<ActionResult<ApiResponse<object>>> Assign([FromBody] AssignPermissionsRequest request)
    {
        var response = await _permissionService.AssignAsync(request, _currentUser.UserId);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("check")]
    public async Task<ActionResult<ApiResponse<bool>>> Check([FromQuery] string module, [FromQuery] string action)
    {
        var response = await _permissionService.HasPermissionAsync(_currentUser.UserId, module, action);
        return Ok(response);
    }
}
