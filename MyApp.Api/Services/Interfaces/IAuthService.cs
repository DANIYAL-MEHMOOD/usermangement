using MyApp.Api.Common;
using MyApp.Api.DTOs;

namespace MyApp.Api.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, string? ip, string? browser);
    Task<ApiResponse<object>> LogoutAsync(string token, string? ip);
    Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordRequest request, string? ip);
    Task<ApiResponse<object>> ChangePasswordAsync(int userId, ChangePasswordRequest request, string? currentToken);
    Task<ApiResponse<List<SessionInfoDto>>> GetSessionsAsync(int userId, string? currentToken);
    Task<ApiResponse<object>> RevokeSessionAsync(int userId, int sessionTokenId, string? currentToken);
    Task<ApiResponse<object>> RevokeOtherSessionsAsync(int userId, string? currentToken);
}
