using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Security;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var browser = Request.Headers.UserAgent.ToString();
        var response = await _authService.LoginAsync(request, ip, browser);
        if (!response.Success) return Unauthorized(response);
        return Ok(response);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var response = await _authService.LogoutAsync(request.Token, ip);
        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var response = await _authService.ForgotPasswordAsync(request);
        return Ok(response);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var response = await _authService.ResetPasswordAsync(request, ip);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var response = await _authService.ChangePasswordAsync(_currentUser.UserId!.Value, request, CurrentToken());
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<SessionInfoDto>>>> GetSessions()
    {
        var response = await _authService.GetSessionsAsync(_currentUser.UserId!.Value, CurrentToken());
        return Ok(response);
    }

    [HttpDelete("sessions/current")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> RevokeCurrentSession()
    {
        var response = await _authService.LogoutAsync(CurrentToken(), _currentUser.IpAddress);
        return Ok(response);
    }

    [HttpDelete("sessions/{id:int}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> RevokeSession(int id)
    {
        var response = await _authService.RevokeSessionAsync(_currentUser.UserId!.Value, id, CurrentToken());
        if (!response.Success) return NotFound(response);
        return Ok(response);
    }

    [HttpDelete("sessions/others")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> RevokeOtherSessions()
    {
        var response = await _authService.RevokeOtherSessionsAsync(_currentUser.UserId!.Value, CurrentToken());
        return Ok(response);
    }

    private string CurrentToken()
    {
        var value = Request.Headers[SessionTokenDefaults.HeaderName].FirstOrDefault();
        return string.IsNullOrEmpty(value) ? string.Empty : value;
    }
}
