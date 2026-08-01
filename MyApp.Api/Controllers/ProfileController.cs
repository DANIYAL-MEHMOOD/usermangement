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
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public ProfileController(IProfileService profileService, IAuthService authService, ICurrentUserService currentUser)
    {
        _profileService = profileService;
        _authService = authService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ProfileDto?>>> Get()
    {
        var response = await _profileService.GetProfileAsync(_currentUser.UserId);
        if (!response.Success) return NotFound(response);
        return Ok(response);
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<object>>> Update([FromBody] UpdateProfileDto request)
    {
        var response = await _profileService.UpdateProfileAsync(_currentUser.UserId, request, _currentUser.UserId);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<ApiResponse<UserPreferences?>>> GetPreferences()
    {
        var response = await _profileService.GetPreferencesAsync(_currentUser.UserId);
        return Ok(response);
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<ApiResponse<object>>> UpdatePreferences([FromBody] UpdatePreferencesDto request)
    {
        var response = await _profileService.UpdatePreferencesAsync(_currentUser.UserId, request);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var response = await _authService.ChangePasswordAsync(_currentUser.UserId, request);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }
}
