using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MyApp.Api.Configurations;
using MyApp.Api.Data;
using MyApp.Api.Models;

namespace MyApp.Api.Security;

/// <summary>
/// Session token lifecycle backed by stored procedures. All database access
/// goes through the shared <see cref="SqlDataAccess"/> helper; no repository layer.
/// </summary>
public class SessionTokenService : ISessionTokenService
{
    private readonly SqlDataAccess _db;
    private readonly SecuritySettings _security;

    public SessionTokenService(SqlDataAccess db, IOptions<SecuritySettings> security)
    {
        _db = db;
        _security = security.Value;
    }

    public async Task<(string Token, DateTime Expiry)> CreateAsync(int userId, string? ipAddress, string? userAgent, bool rememberMe, CancellationToken ct = default)
    {
        var token = GenerateToken();
        var tokenHash = HashToken(token);
        var expiry = DateTime.UtcNow.AddHours(rememberMe ? _security.SessionTokenRememberDays * 24 : _security.SessionTokenHours);

        await _db.ExecuteNonQueryAsync("dbo.sp_SaveSessionToken", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@TokenHash", tokenHash);
            SqlDataAccess.AddParam(cmd, "@ExpiryDate", expiry);
            SqlDataAccess.AddParam(cmd, "@IpAddress", ipAddress);
            SqlDataAccess.AddParam(cmd, "@UserAgent", userAgent);
        }, ct);

        return (token, expiry);
    }

    public Task<SessionTokenPrincipal?> ValidateAsync(string token, CancellationToken ct = default)
    {
        var tokenHash = HashToken(token);
        return _db.ExecuteReaderSingleAsync("dbo.sp_GetSessionByTokenHash", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@TokenHash", tokenHash);
        }, reader => new SessionTokenPrincipal
        {
            SessionTokenId = reader.GetInt32(reader.GetOrdinal("SessionTokenId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
            RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
            ExpiryDate = reader.GetDateTime(reader.GetOrdinal("ExpiryDate"))
        }, ct);
    }

    public Task RevokeAsync(string token, CancellationToken ct = default)
    {
        var tokenHash = HashToken(token);
        return _db.ExecuteNonQueryAsync("dbo.sp_RevokeSessionByTokenHash", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@TokenHash", tokenHash);
        }, ct);
    }

    public async Task<List<SessionToken>> GetActiveSessionsAsync(int userId, string? currentToken, CancellationToken ct = default)
    {
        var sessions = await _db.ExecuteReaderAsync("dbo.sp_GetUserSessions", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
        }, reader => new SessionToken
        {
            SessionTokenId = reader.GetInt32(reader.GetOrdinal("SessionTokenId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            // Hash is loaded internally only to detect the current session; it
            // is never exposed to callers (the API maps to SessionInfoDto).
            TokenHash = reader.GetString(reader.GetOrdinal("TokenHash")),
            ExpiryDate = reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
            CreatedByIp = reader.GetNullableString("CreatedByIp"),
            UserAgent = reader.GetNullableString("UserAgent"),
            IsRevoked = false,
            RevokedDate = null
        }, ct);

        var currentHash = string.IsNullOrEmpty(currentToken) ? null : HashToken(currentToken);
        foreach (var session in sessions)
        {
            session.IsCurrent = currentHash is not null && session.TokenHash == currentHash;
        }
        return sessions;
    }

    public Task RevokeSessionAsync(int sessionTokenId, int userId, CancellationToken ct = default) =>
        _db.ExecuteNonQueryAsync("dbo.sp_RevokeSession", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@SessionTokenId", sessionTokenId);
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
        }, ct);

    public Task RevokeAllExceptAsync(int userId, string? currentToken, CancellationToken ct = default)
    {
        var currentHash = string.IsNullOrEmpty(currentToken) ? null : HashToken(currentToken);
        return _db.ExecuteNonQueryAsync("dbo.sp_RevokeAllUserSessions", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@ExcludeTokenHash", currentHash);
        }, ct);
    }

    public Task RevokeAllAsync(int userId, CancellationToken ct = default) =>
        _db.ExecuteNonQueryAsync("dbo.sp_RevokeAllUserSessions", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@ExcludeTokenHash", null);
        }, ct);

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
