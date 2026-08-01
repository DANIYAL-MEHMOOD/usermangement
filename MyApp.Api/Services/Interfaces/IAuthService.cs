using MyApp.Api.Common;
using MyApp.Api.DTOs;

namespace MyApp.Api.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, string? ip, string? browser);
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, string? ip);
    Task<ApiResponse<object>> LogoutAsync(string refreshToken, string? ip);
    Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ApiResponse<object>> ChangePasswordAsync(int userId, ChangePasswordRequest request);
}
