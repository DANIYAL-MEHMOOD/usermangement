namespace MyApp.Api.DTOs;

public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public bool RememberMe { get; set; }
}

/// <summary>
/// Successfully authenticated session. <see cref="Token"/> is an opaque,
/// randomly generated session token (only ever returned once at login) that
/// the Web layer stores server-side in its own session and forwards on every
/// API call via the <c>X-Api-Token</c> header. Never JWT, never exposed to
/// browser JavaScript.
/// </summary>
public record LoginResponse(
    string Token, DateTime TokenExpiry,
    int UserId, string Username, string FullName, string RoleName, bool MustChangePassword);

public class LogoutRequest
{
    public required string Token { get; set; }
}

public class ForgotPasswordRequest
{
    public required string Email { get; set; }
}

public class ResetPasswordRequest
{
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmNewPassword { get; set; }
}

public class ChangePasswordRequest
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmNewPassword { get; set; }
}

/// <summary>A single active login session for the current user (Session Management module).</summary>
public record SessionInfoDto(
    int SessionTokenId, DateTime CreatedDate, DateTime ExpiryDate,
    string? IpAddress, string? UserAgent, bool IsCurrent);
