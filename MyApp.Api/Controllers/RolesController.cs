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
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ICurrentUserService _currentUser;

    public RolesController(IRoleService roleService, ICurrentUserService currentUser)
    {
        _roleService = roleService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleItem>>>> GetAll([FromQuery] bool includeSystem = true)
    {
        var response = await _roleService.GetAllAsync(includeSystem);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<Role?>>> GetById(int id)
    {
        var response = await _roleService.GetByIdAsync(id);
        if (!response.Success) return NotFound(response);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Save([FromBody] SaveRoleRequest request)
    {
        var response = await _roleService.SaveAsync(request, _currentUser.UserId);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var response = await _roleService.DeleteAsync(id, _currentUser.UserId);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("clone")]
    public async Task<ActionResult<ApiResponse<int>>> Clone([FromBody] CloneRoleRequest request)
    {
        var response = await _roleService.CloneAsync(request, _currentUser.UserId);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPatch("{userId:int}/assign/{roleId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Assign(int userId, int roleId)
    {
        var response = await _roleService.AssignRoleAsync(userId, roleId, _currentUser.UserId);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }
}
