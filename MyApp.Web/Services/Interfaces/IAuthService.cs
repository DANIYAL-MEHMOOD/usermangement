using MyApp.Web.Common;
using MyApp.Web.DTOs;

namespace MyApp.Web.Services.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }
    string Username { get; }
    string FullName { get; }
    string RoleName { get; }
    /// <summary>The opaque API session token stored in the server-side session.</summary>
    string ApiToken { get; }
    bool IsAuthenticated { get; }
    bool IsAdministrator { get; }
}

public interface IAuthService
{
    Task<ApiResponse<LoginResponseDto>?> LoginAsync(LoginRequestDto request);
    Task<ApiResponse<object>?> LogoutAsync();
    Task<ApiResponse<object>?> ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<ApiResponse<object>?> ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<ApiResponse<object>?> ChangePasswordAsync(ChangePasswordRequestDto request);

    // Session Management
    Task<ApiResponse<List<SessionInfoDto>>?> GetSessionsAsync();
    Task<ApiResponse<object>?> RevokeSessionAsync(int sessionTokenId);
    Task<ApiResponse<object>?> RevokeOtherSessionsAsync();
}
