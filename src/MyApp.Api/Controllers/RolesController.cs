using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Common;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;

namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // read endpoints open to any signed-in user; mutations below are locked to Administrator
public class RolesController : ControllerBase
{
    private readonly IRoleRepository _roles;
    private readonly ICurrentUserService _currentUser;

    public RolesController(IRoleRepository roles, ICurrentUserService currentUser)
    {
        _roles = roles;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> Search([FromQuery] string? searchTerm, [FromQuery] bool? isActive)
    {
        var roles = await _roles.SearchAsync(searchTerm, isActive);
        var dtos = roles.Select(r => new RoleDto(r.RoleId, r.RoleName, r.Description, r.IsSystemRole, r.IsActive, r.UserCount)).ToList();
        return Ok(ApiResponse<List<RoleDto>>.Ok(dtos));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(int id)
    {
        var role = await _roles.GetByIdAsync(id);
        if (role is null) return NotFound(ApiResponse<object>.Fail("Role not found."));
        return Ok(ApiResponse<object>.Ok(role));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> Save(SaveRoleRequest request)
    {
        var userId = _currentUser.UserId ?? 0;
        try
        {
            var roleId = await _roles.SaveAsync(request.RoleId, request.RoleName, request.Description, request.IsActive, userId);
            return Ok(ApiResponse<object>.Ok(new { RoleId = roleId }, "Role saved."));
        }
        catch (Exception ex) when (ex.Message.Contains("already exists"))
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        try
        {
            await _roles.DeleteAsync(id, _currentUser.UserId ?? 0);
            return Ok(ApiResponse<object>.Ok(new { }, "Role deleted."));
        }
        catch (Exception ex) when (ex.Message.Contains("cannot") || ex.Message.Contains("Cannot"))
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("clone")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> Clone(CloneRoleRequest request)
    {
        var userId = _currentUser.UserId ?? 0;
        try
        {
            var roleId = await _roles.CloneAsync(request.SourceRoleId, request.NewRoleName, request.Description, userId);
            return Ok(ApiResponse<object>.Ok(new { RoleId = roleId }, "Role cloned with permissions copied."));
        }
        catch (Exception ex) when (ex.Message.Contains("already exists"))
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPatch("{userId:int}/assign/{roleId:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> AssignToUser(int userId, int roleId,
        [FromServices] IUserRepository users)
    {
        await users.AssignRoleAsync(userId, roleId, _currentUser.UserId ?? 0);
        return Ok(ApiResponse<object>.Ok(new { }, "Role assigned."));
    }
}
