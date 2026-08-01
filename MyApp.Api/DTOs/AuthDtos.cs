namespace MyApp.Api.DTOs;

public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public bool RememberMe { get; set; }
}

public record LoginResponse(
    string AccessToken, string RefreshToken, DateTime AccessTokenExpiry,
    int UserId, string Username, string FullName, string RoleName, bool MustChangePassword);

public class RefreshTokenRequest
{
    public required string RefreshToken { get; set; }
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
