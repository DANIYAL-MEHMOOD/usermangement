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
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public UsersController(IUserService userService, ICurrentUserService currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserListItemDto>>>> Search([FromQuery] UserSearchRequest request)
    {
        var response = await _userService.SearchAsync(request);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<User?>>> GetById(int id)
    {
        var response = await _userService.GetByIdAsync(id);
        if (!response.Success) return NotFound(response);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Create([FromBody] CreateUserRequest request)
    {
        var response = await _userService.CreateAsync(request, _currentUser.UserId);
        if (!response.Success) return BadRequest(response);
        return CreatedAtAction(nameof(GetById), new { id = response.Data }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var response = await _userService.UpdateAsync(id, request, _currentUser.UserId);
        if (!response.Success) return NotFound(response);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var response = await _userService.DeleteAsync(id, _currentUser.UserId);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPatch("{id:int}/activate")]
    public async Task<ActionResult<ApiResponse<object>>> Activate(int id)
    {
        var response = await _userService.SetStatusAsync(id, 1, _currentUser.UserId);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPatch("{id:int}/deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(int id)
    {
        var response = await _userService.SetStatusAsync(id, 0, _currentUser.UserId);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<ApiResponse<UserPreferences?>>> GetPreferences()
    {
        var response = await _userService.GetPreferencesAsync(_currentUser.UserId);
        return Ok(response);
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<ApiResponse<object>>> SavePreferences([FromBody] UserPreferences preferences)
    {
        var response = await _userService.SavePreferencesAsync(_currentUser.UserId, preferences);
        return Ok(response);
    }
}
