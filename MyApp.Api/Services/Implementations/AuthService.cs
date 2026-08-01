using Microsoft.Extensions.Options;
using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Models;
using MyApp.Api.Repository.Interfaces;
using MyApp.Api.Security;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepo;
    private readonly IAuditLogRepository _auditLog;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IAuthRepository authRepo,
        IAuditLogRepository auditLog,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtGenerator,
        IOptions<JwtSettings> jwtSettings)
    {
        _authRepo = authRepo;
        _auditLog = auditLog;
        _passwordHasher = passwordHasher;
        _jwtGenerator = jwtGenerator;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, string? ip, string? browser)
    {
        await _authRepo.UnlockExpiredLockoutsAsync();

        var user = await _authRepo.GetForLoginAsync(request.Username);

        if (user is null)
            return ApiResponse<LoginResponse>.Fail("Invalid username or password.");

        if (user.IsLocked && user.LockoutEnd is DateTime lockoutEnd && lockoutEnd > DateTime.UtcNow)
            return ApiResponse<LoginResponse>.Fail("Account is locked. Try again later.");

        if (user.Status != 1)
            return ApiResponse<LoginResponse>.Fail("Account is not active.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            await _authRepo.RecordLoginFailureAsync(user.UserId);
            await _auditLog.InsertAsync(user.UserId, "Auth", "LoginFailed", null, null, browser, ip);
            return ApiResponse<LoginResponse>.Fail("Invalid username or password.");
        }

        await _authRepo.RecordLoginSuccessAsync(user.UserId);

        var (access, expiry) = _jwtGenerator.GenerateAccessToken(user.UserId, user.Username, user.RoleName);
        var refresh = _jwtGenerator.GenerateRefreshToken();
        var refreshExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

        await _authRepo.SaveRefreshTokenAsync(user.UserId, refresh, refreshExpiry, ip);
        await _auditLog.InsertAsync(user.UserId, "Auth", "LoginSuccess", null, null, browser, ip);

        var response = new LoginResponse(
            access,
            refresh,
            expiry,
            user.UserId,
            user.Username,
            user.FullName,
            user.RoleName,
            user.MustChangePassword
        );

        return ApiResponse<LoginResponse>.Ok(response, "Login successful.");
    }

    public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, string? ip)
    {
        var stored = await _authRepo.GetRefreshTokenAsync(request.RefreshToken);

        if (stored is null || !stored.IsActive)
            return ApiResponse<LoginResponse>.Fail("Invalid or expired refresh token.");

        var user = await _authRepo.GetForLoginAsync(stored.UserId.ToString());
        if (user is null || user.Status != 1 || user.IsDeleted)
            return ApiResponse<LoginResponse>.Fail("User account is not active.");

        var newRefresh = _jwtGenerator.GenerateRefreshToken();
        var newExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

        await _authRepo.RevokeRefreshTokenAsync(request.RefreshToken, ip, newRefresh);
        await _authRepo.SaveRefreshTokenAsync(user.UserId, newRefresh, newExpiry, ip);

        var (access, expiry) = _jwtGenerator.GenerateAccessToken(user.UserId, user.Username, user.RoleName);

        var response = new LoginResponse(
            access,
            newRefresh,
            expiry,
            user.UserId,
            user.Username,
            user.FullName,
            user.RoleName,
            user.MustChangePassword
        );

        return ApiResponse<LoginResponse>.Ok(response, "Token refreshed successfully.");
    }

    public async Task<ApiResponse<object>> LogoutAsync(string refreshToken, string? ip)
    {
        await _authRepo.RevokeRefreshTokenAsync(refreshToken, ip);
        return ApiResponse<object>.Ok(null, "Logged out successfully.");
    }

    public async Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        // For security, always return success message even if email is not found
        return ApiResponse<object>.Ok(null, "If your email is registered, you will receive a password reset link.");
    }

    public async Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return ApiResponse<object>.Fail("Passwords do not match.");

        var tokenData = await _authRepo.ValidatePasswordResetTokenAsync(request.Token);
        if (tokenData is null || tokenData.Value.IsUsed || tokenData.Value.ExpiryDate < DateTime.UtcNow)
            return ApiResponse<object>.Fail("Invalid or expired password reset token.");

        var (hash, salt) = _passwordHasher.Hash(request.NewPassword);
        await _authRepo.ChangePasswordAsync(tokenData.Value.UserId, hash, salt);
        await _authRepo.ConsumePasswordResetTokenAsync(tokenData.Value.TokenId);

        return ApiResponse<object>.Ok(null, "Password reset successfully.");
    }

    public async Task<ApiResponse<object>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return ApiResponse<object>.Fail("New passwords do not match.");

        var (hash, salt) = _passwordHasher.Hash(request.NewPassword);
        await _authRepo.ChangePasswordAsync(userId, hash, salt);

        return ApiResponse<object>.Ok(null, "Password changed successfully.");
    }
}
