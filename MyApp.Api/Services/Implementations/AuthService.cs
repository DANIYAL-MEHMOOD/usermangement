using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MyApp.Api.Common;
using MyApp.Api.Configurations;
using MyApp.Api.Data;
using MyApp.Api.DTOs;
using MyApp.Api.Helpers;
using MyApp.Api.Models;
using MyApp.Api.Security;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

/// <summary>
/// Authentication service: login/logout, account lockout, password change and
/// reset, and session management. All database access uses stored procedures
/// through the centralized <see cref="SqlDataAccess"/> component — no EF, no
/// Dapper, no repository layer, no raw SQL.
/// </summary>
public class AuthService : IAuthService
{
    private readonly SqlDataAccess _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionTokenService _sessions;
    private readonly SecuritySettings _security;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        SqlDataAccess db,
        IPasswordHasher passwordHasher,
        ISessionTokenService sessions,
        IOptions<SecuritySettings> security,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _sessions = sessions;
        _security = security.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, string? ip, string? browser)
    {
        await _db.ExecuteNonQueryAsync("dbo.sp_UnlockExpiredLockouts", _ => { });

        var user = await GetForLoginAsync(request.Username);

        if (user is null)
            return ApiResponse<LoginResponse>.Fail("Invalid username or password.", 401);

        if (user.IsLocked && user.LockoutEnd is DateTime lockoutEnd && lockoutEnd > DateTime.UtcNow)
            return ApiResponse<LoginResponse>.Fail("Account is locked. Try again later.", 423);

        if (user.Status != 1)
            return ApiResponse<LoginResponse>.Fail("Account is not active.", 403);

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            await RecordLoginFailureAsync(user.UserId);
            await LogAuditAsync(user.UserId, "Auth", "LoginFailed", null, "Invalid password attempt", browser, ip);
            return ApiResponse<LoginResponse>.Fail("Invalid username or password.", 401);
        }

        await RecordLoginSuccessAsync(user.UserId);
        await LogAuditAsync(user.UserId, "Auth", "LoginSuccess", null, null, browser, ip);

        var (token, expiry) = await _sessions.CreateAsync(user.UserId, ip, browser, request.RememberMe);

        var response = new LoginResponse(
            token, expiry,
            user.UserId, user.Username, user.FullName, user.RoleName, user.MustChangePassword);

        return ApiResponse<LoginResponse>.Ok(response, "Login successful.");
    }

    public async Task<ApiResponse<object>> LogoutAsync(string token, string? ip)
    {
        var session = await _sessions.ValidateAsync(token);
        await _sessions.RevokeAsync(token);
        await LogAuditAsync(session?.UserId, "Auth", "Logout", null, "Session token revoked", null, ip);
        return ApiResponse<object>.Ok(null, "Logged out successfully.");
    }

    public async Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        // Always answer with the same generic message even when the email is
        // unknown, so the endpoint cannot be used to enumerate accounts.
        const string genericMessage = "If your email is registered, you will receive a password reset link.";

        var user = await FindUserByEmailAsync(request.Email);
        if (user is null)
        {
            _logger.LogInformation("Password reset requested for unknown email {Email}.", request.Email);
            return ApiResponse<object>.Ok(null, genericMessage);
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        await _db.ExecuteNonQueryAsync("dbo.sp_CreatePasswordResetToken", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", user.UserId);
            SqlDataAccess.AddParam(cmd, "@Token", token);
            SqlDataAccess.AddParam(cmd, "@ExpiryMinutes", 30);
        });

        await LogAuditAsync(user.UserId, "Auth", "PasswordResetRequested", null, null, null, null);

        if (_security.ExposeResetLinkInResponse)
        {
            // Development convenience: return the link so the flow can be
            // exercised without a mail server. Production deployments keep
            // ExposeResetLinkInResponse=false and email the link instead.
            return ApiResponse<object>.Ok(new { ResetLink = $"/Auth/ResetPassword?token={token}" }, genericMessage);
        }

        return ApiResponse<object>.Ok(null, genericMessage);
    }

    public async Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordRequest request, string? ip)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return ApiResponse<object>.Fail("Passwords do not match.");

        if (!PasswordPolicyHelper.ValidatePassword(request.NewPassword, out var policyError))
            return ApiResponse<object>.Fail(policyError);

        var tokenData = await _db.ExecuteReaderSingleAsync("dbo.sp_ValidatePasswordResetToken",
            cmd => SqlDataAccess.AddParam(cmd, "@Token", request.Token),
            reader => (
                TokenId: reader.GetInt32(0),
                UserId: reader.GetInt32(1),
                ExpiryDate: reader.GetDateTime(2),
                IsUsed: reader.GetBoolean(3)));

        if (tokenData is null || tokenData.Value.IsUsed || tokenData.Value.ExpiryDate < DateTime.UtcNow)
            return ApiResponse<object>.Fail("Invalid or expired password reset token.");

        var userId = tokenData.Value.UserId;

        if (await PasswordInHistoryAsync(userId, request.NewPassword))
            return ApiResponse<object>.Fail("You cannot reuse a recently used password.");

        var (hash, salt) = _passwordHasher.Hash(request.NewPassword);
        await ChangePasswordAsync(userId, hash, salt);

        await _db.ExecuteNonQueryAsync("dbo.sp_ConsumePasswordResetToken",
            cmd => SqlDataAccess.AddParam(cmd, "@TokenId", tokenData.Value.TokenId));

        // A reset password invalidates every existing session (security best practice).
        await _sessions.RevokeAllAsync(userId);
        await LogAuditAsync(userId, "Auth", "PasswordResetCompleted", null, null, null, ip);

        return ApiResponse<object>.Ok(null, "Password reset successfully.");
    }

    public async Task<ApiResponse<object>> ChangePasswordAsync(int userId, ChangePasswordRequest request, string? currentToken)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return ApiResponse<object>.Fail("New passwords do not match.");

        if (!PasswordPolicyHelper.ValidatePassword(request.NewPassword, out var policyError))
            return ApiResponse<object>.Fail(policyError);

        var credentials = await GetPasswordCredentialsAsync(userId);
        if (credentials is null)
            return ApiResponse<object>.Fail("User not found.", 404);

        if (!_passwordHasher.Verify(request.CurrentPassword, credentials.Value.Hash, credentials.Value.Salt))
            return ApiResponse<object>.Fail("Current password is incorrect.");

        if (await PasswordInHistoryAsync(userId, request.NewPassword))
            return ApiResponse<object>.Fail("You cannot reuse a recently used password.");

        var (hash, salt) = _passwordHasher.Hash(request.NewPassword);
        await ChangePasswordAsync(userId, hash, salt);

        // Keep the current session alive, sign out everywhere else.
        await _sessions.RevokeAllExceptAsync(userId, currentToken);
        await LogAuditAsync(userId, "Auth", "PasswordChanged", null, null, null, null);

        return ApiResponse<object>.Ok(null, "Password changed successfully.");
    }

    public async Task<ApiResponse<List<SessionInfoDto>>> GetSessionsAsync(int userId, string? currentToken)
    {
        var sessions = await _sessions.GetActiveSessionsAsync(userId, currentToken);
        var items = sessions
            .OrderByDescending(s => s.IsCurrent)
            .ThenByDescending(s => s.CreatedDate)
            .Select(s => new SessionInfoDto(
                s.SessionTokenId, s.CreatedDate, s.ExpiryDate, s.CreatedByIp, s.UserAgent, s.IsCurrent))
            .ToList();

        return ApiResponse<List<SessionInfoDto>>.Ok(items);
    }

    public async Task<ApiResponse<object>> RevokeSessionAsync(int userId, int sessionTokenId, string? currentToken)
    {
        var sessions = await _sessions.GetActiveSessionsAsync(userId, currentToken);
        var target = sessions.FirstOrDefault(s => s.SessionTokenId == sessionTokenId);
        if (target is null)
            return ApiResponse<object>.Fail("Session not found.", 404);

        await _sessions.RevokeSessionAsync(sessionTokenId, userId);
        await LogAuditAsync(userId, "Auth", "SessionRevoked",
            $"SessionTokenId={sessionTokenId}", target.IsCurrent ? "Current session revoked" : "Session revoked", null, null);

        return ApiResponse<object>.Ok(null, "Session revoked successfully.");
    }

    public async Task<ApiResponse<object>> RevokeOtherSessionsAsync(int userId, string? currentToken)
    {
        await _sessions.RevokeAllExceptAsync(userId, currentToken);
        await LogAuditAsync(userId, "Auth", "SessionsRevoked", null, "All other sessions revoked", null, null);
        return ApiResponse<object>.Ok(null, "All other sessions have been signed out.");
    }

    // ------------------------------------------------------------------
    // Database access (stored procedures only, via SqlDataAccess)
    // ------------------------------------------------------------------

    private async Task<User?> GetForLoginAsync(string username) =>
        await _db.ExecuteReaderSingleAsync("dbo.sp_Login",
            cmd => SqlDataAccess.AddParam(cmd, "@Username", username),
            MapUser);

    private async Task<(int UserId)?> FindUserByEmailAsync(string email) =>
        await _db.ExecuteReaderSingleAsync("dbo.sp_FindUserByEmail",
            cmd => SqlDataAccess.AddParam(cmd, "@Email", email),
            reader => (UserId: reader.GetInt32(reader.GetOrdinal("UserId"))));

    private async Task<(byte[] Hash, byte[] Salt)?> GetPasswordCredentialsAsync(int userId) =>
        await _db.ExecuteReaderSingleAsync("dbo.sp_GetPasswordHash",
            cmd => SqlDataAccess.AddParam(cmd, "@UserId", userId),
            reader => (
                Hash: (byte[])reader["PasswordHash"],
                Salt: (byte[])reader["PasswordSalt"]));

    private async Task<bool> PasswordInHistoryAsync(int userId, string candidate)
    {
        var history = await _db.ExecuteReaderAsync("dbo.sp_CheckPasswordHistory",
            cmd =>
            {
                SqlDataAccess.AddParam(cmd, "@UserId", userId);
                SqlDataAccess.AddParam(cmd, "@HistoryCount", 5);
            },
            reader => (
                Hash: (byte[])reader["PasswordHash"],
                Salt: (byte[])reader["PasswordSalt"]));

        return history.Any(h => _passwordHasher.Verify(candidate, h.Hash, h.Salt));
    }

    private async Task ChangePasswordAsync(int userId, byte[] hash, byte[] salt) =>
        await _db.ExecuteNonQueryAsync("dbo.sp_ChangePassword", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@NewPasswordHash", hash, SqlDbType.VarBinary);
            SqlDataAccess.AddParam(cmd, "@NewPasswordSalt", salt, SqlDbType.VarBinary);
            SqlDataAccess.AddParam(cmd, "@ExpiryDays", _security.PasswordExpiryDays);
        });

    private async Task RecordLoginSuccessAsync(int userId) =>
        await _db.ExecuteNonQueryAsync("dbo.sp_RecordLoginSuccess",
            cmd => SqlDataAccess.AddParam(cmd, "@UserId", userId));

    private async Task RecordLoginFailureAsync(int userId) =>
        await _db.ExecuteNonQueryAsync("dbo.sp_RecordLoginFailure", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@MaxAttempts", _security.MaxFailedLoginAttempts);
            SqlDataAccess.AddParam(cmd, "@LockoutMinutes", _security.AccountLockoutMinutes);
        });

    private async Task LogAuditAsync(int? userId, string module, string action, string? oldValue, string? newValue, string? browser, string? ipAddress) =>
        await _db.ExecuteNonQueryAsync("dbo.sp_InsertAuditLog", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@Module", module);
            SqlDataAccess.AddParam(cmd, "@Action", action);
            SqlDataAccess.AddParam(cmd, "@OldValue", oldValue);
            SqlDataAccess.AddParam(cmd, "@NewValue", newValue);
            SqlDataAccess.AddParam(cmd, "@Browser", browser);
            SqlDataAccess.AddParam(cmd, "@IPAddress", ipAddress);
        });

    private static User MapUser(SqlDataReader reader) => new()
    {
        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
        Username = reader.GetString(reader.GetOrdinal("Username")),
        PasswordHash = reader.HasColumn("PasswordHash") && !reader.IsDBNull(reader.GetOrdinal("PasswordHash"))
            ? (byte[])reader["PasswordHash"] : [],
        PasswordSalt = reader.HasColumn("PasswordSalt") && !reader.IsDBNull(reader.GetOrdinal("PasswordSalt"))
            ? (byte[])reader["PasswordSalt"] : [],
        FullName = reader.GetString(reader.GetOrdinal("FullName")),
        Email = reader.HasColumn("Email") ? reader.GetString(reader.GetOrdinal("Email")) : string.Empty,
        RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
        RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
        Status = reader.HasColumn("Status") ? Convert.ToByte(reader["Status"]) : (byte)0,
        IsLocked = reader.HasColumn("IsLocked") && reader.GetBoolean(reader.GetOrdinal("IsLocked")),
        LockoutEnd = reader.HasColumn("LockoutEnd") && !reader.IsDBNull(reader.GetOrdinal("LockoutEnd"))
            ? reader.GetDateTime(reader.GetOrdinal("LockoutEnd")) : null,
        FailedLoginAttempts = reader.HasColumn("FailedLoginAttempts") ? reader.GetInt32(reader.GetOrdinal("FailedLoginAttempts")) : 0,
        PasswordExpiryDate = reader.HasColumn("PasswordExpiryDate") && !reader.IsDBNull(reader.GetOrdinal("PasswordExpiryDate"))
            ? reader.GetDateTime(reader.GetOrdinal("PasswordExpiryDate")) : null,
        MustChangePassword = reader.HasColumn("MustChangePassword") && reader.GetBoolean(reader.GetOrdinal("MustChangePassword")),
        LastLogin = reader.HasColumn("LastLogin") && !reader.IsDBNull(reader.GetOrdinal("LastLogin"))
            ? reader.GetDateTime(reader.GetOrdinal("LastLogin")) : null,
        LoginCount = reader.HasColumn("LoginCount") ? reader.GetInt32(reader.GetOrdinal("LoginCount")) : 0,
        CreatedDate = reader.HasColumn("CreatedDate") ? reader.GetDateTime(reader.GetOrdinal("CreatedDate")) : default
    };
}
