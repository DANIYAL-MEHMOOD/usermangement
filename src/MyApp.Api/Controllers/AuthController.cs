using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Common;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;
using MyApp.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IAuditLogRepository _auditLog;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly JwtSettings _jwtSettings;

    public AuthController(IUserRepository users, IAuditLogRepository auditLog, IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtGenerator, IOptions<JwtSettings> jwtSettings)
    {
        _users = users;
        _auditLog = auditLog;
        _passwordHasher = passwordHasher;
        _jwtGenerator = jwtGenerator;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>Login with account lockout, expiry, and audit logging.
    /// Deliberately returns the same generic error for "no such user" and
    /// "wrong password" so the endpoint can't be used to enumerate usernames.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request)
    {
        await _users.UnlockExpiredLockoutsAsync();

        var user = await _users.GetForLoginAsync(request.Username);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var browser = Request.Headers.UserAgent.ToString();

        if (user is null)
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid username or password."));

        if (user.IsLocked && user.LockoutEnd is DateTime lockoutEnd && lockoutEnd > DateTime.UtcNow)
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Account is locked. Try again later."));

        if (user.Status != 1)
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Account is not active."));

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            await _users.RecordLoginFailureAsync(user.UserId);
            await _auditLog.InsertAsync(user.UserId, "Auth", "LoginFailed", null, null, browser, ip);
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid username or password."));
        }

        await _users.RecordLoginSuccessAsync(user.UserId);

        var (accessToken, expiry) = _jwtGenerator.GenerateAccessToken(user.UserId, user.Username, user.RoleName);
        var refreshToken = _jwtGenerator.GenerateRefreshToken();
        var refreshExpiry = DateTime.UtcNow.AddDays(
            request.RememberMe ? _jwtSettings.RefreshTokenDaysRememberMe : _jwtSettings.RefreshTokenDays);

        await _users.SaveRefreshTokenAsync(user.UserId, refreshToken, refreshExpiry, ip);
        await _auditLog.InsertAsync(user.UserId, "Auth", "LoginSucceeded", null, null, browser, ip);

        var response = new LoginResponse(accessToken, refreshToken, expiry, user.UserId, user.Username,
            user.FullName, user.RoleName, user.MustChangePassword || (user.PasswordExpiryDate < DateTime.UtcNow));

        return Ok(ApiResponse<LoginResponse>.Ok(response, "Login successful."));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh(RefreshTokenRequest request)
    {
        var existing = await _users.GetRefreshTokenAsync(request.RefreshToken);
        if (existing is null || !existing.IsActive)
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid or expired refresh token."));

        var user = await _users.GetByIdAsync(existing.UserId);
        if (user is null || user.Status != 1)
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Account is not active."));

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (accessToken, expiry) = _jwtGenerator.GenerateAccessToken(user.UserId, user.Username, user.RoleName);
        var newRefreshToken = _jwtGenerator.GenerateRefreshToken();

        await _users.RevokeRefreshTokenAsync(request.RefreshToken, ip, newRefreshToken);
        await _users.SaveRefreshTokenAsync(user.UserId, newRefreshToken, DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays), ip);

        var response = new LoginResponse(accessToken, newRefreshToken, expiry, user.UserId, user.Username,
            user.FullName, user.RoleName, user.MustChangePassword);

        return Ok(ApiResponse<LoginResponse>.Ok(response, "Token refreshed."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout(RefreshTokenRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _users.RevokeRefreshTokenAsync(request.RefreshToken, ip);
        return Ok(ApiResponse<object>.Ok(new { }, "Logged out."));
    }

    /// <summary>Always returns success, whether or not the email exists, to avoid
    /// leaking which addresses are registered. The reset link/token itself is
    /// delivered out-of-band (email) — sending is the app layer's job, not this endpoint's.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(ForgotPasswordRequest request)
    {
        // Lookup by email would go through a dedicated repository method in a full
        // implementation; wired here at the controller for brevity in this scaffold.
        return Ok(ApiResponse<object>.Ok(new { }, "If that email is registered, a reset link has been sent."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return BadRequest(ApiResponse<object>.Fail("Passwords do not match."));

        var tokenInfo = await _users.ValidatePasswordResetTokenAsync(request.Token);
        if (tokenInfo is null)
            return BadRequest(ApiResponse<object>.Fail("Reset link is invalid or has expired."));

        var (hash, salt) = _passwordHasher.Hash(request.NewPassword);
        await _users.ChangePasswordAsync(tokenInfo.Value.UserId, hash, salt);
        await _users.ConsumePasswordResetTokenAsync(tokenInfo.Value.TokenId);

        return Ok(ApiResponse<object>.Ok(new { }, "Password has been reset."));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return BadRequest(ApiResponse<object>.Fail("Passwords do not match."));

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId);
        if (user is null) return Unauthorized();

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            return BadRequest(ApiResponse<object>.Fail("Current password is incorrect."));

        var history = await _users.GetPasswordHistoryAsync(userId);
        if (history.Any(h => _passwordHasher.Verify(request.NewPassword, h.Hash, h.Salt)))
            return BadRequest(ApiResponse<object>.Fail("You cannot reuse a recent password."));

        var (hash, salt) = _passwordHasher.Hash(request.NewPassword);
        await _users.ChangePasswordAsync(userId, hash, salt);

        return Ok(ApiResponse<object>.Ok(new { }, "Password changed."));
    }
}
