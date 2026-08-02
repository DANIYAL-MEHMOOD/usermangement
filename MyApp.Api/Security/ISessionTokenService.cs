using MyApp.Api.Models;

namespace MyApp.Api.Security;

/// <summary>
/// Issues, validates and revokes opaque session tokens (Session Management).
/// The plaintext token is only ever shown once (at login); the database only
/// stores its SHA-256 hash so a leaked database never exposes usable tokens.
/// </summary>
public interface ISessionTokenService
{
    /// <summary>Creates a new session token for the user and returns the plaintext token exactly once.</summary>
    Task<(string Token, DateTime Expiry)> CreateAsync(int userId, string? ipAddress, string? userAgent, bool rememberMe, CancellationToken ct = default);

    /// <summary>Validates a plaintext token and returns the resolved session principal, or null.</summary>
    Task<SessionTokenPrincipal?> ValidateAsync(string token, CancellationToken ct = default);

    /// <summary>Revokes the session identified by a plaintext token (logout).</summary>
    Task RevokeAsync(string token, CancellationToken ct = default);

    /// <summary>Lists the user's active sessions; the session whose hash matches is flagged as current.</summary>
    Task<List<SessionToken>> GetActiveSessionsAsync(int userId, string? currentToken, CancellationToken ct = default);

    /// <summary>Revokes one of the user's sessions (only sessions owned by the user).</summary>
    Task RevokeSessionAsync(int sessionTokenId, int userId, CancellationToken ct = default);

    /// <summary>Revokes every session except the current one ("sign out everywhere else").</summary>
    Task RevokeAllExceptAsync(int userId, string? currentToken, CancellationToken ct = default);

    /// <summary>Revokes every session for the user (password reset / forced logout).</summary>
    Task RevokeAllAsync(int userId, CancellationToken ct = default);
}

/// <summary>Resolved identity for an authenticated API request.</summary>
public class SessionTokenPrincipal
{
    public required int SessionTokenId { get; init; }
    public required int UserId { get; init; }
    public required string Username { get; init; }
    public required int RoleId { get; init; }
    public required string RoleName { get; init; }
    public required DateTime ExpiryDate { get; init; }
}
