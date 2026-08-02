using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Api.Common;
using MyApp.Api.DTOs;
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
        var response = await _profileService.GetProfileAsync(_currentUser.UserId!.Value);
        if (!response.Success) return NotFound(response);
        return Ok(response);
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<object>>> Update([FromBody] UpdateProfileDto request)
    {
        var response = await _profileService.UpdateProfileAsync(_currentUser.UserId!.Value, request, _currentUser.UserId!.Value);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<ApiResponse<Models.UserPreferences?>>> GetPreferences()
    {
        var response = await _profileService.GetPreferencesAsync(_currentUser.UserId!.Value);
        return Ok(response);
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<ApiResponse<object>>> UpdatePreferences([FromBody] UpdatePreferencesDto request)
    {
        var response = await _profileService.UpdatePreferencesAsync(_currentUser.UserId!.Value, request);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var response = await _authService.ChangePasswordAsync(_currentUser.UserId!.Value, request, string.Empty);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }
}
