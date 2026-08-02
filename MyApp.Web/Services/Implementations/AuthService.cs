using MyApp.Web.ApiClient;
using MyApp.Web.Common;
using MyApp.Web.DTOs;
using MyApp.Web.Services.Interfaces;

namespace MyApp.Web.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IApiClient _api;
    private readonly IHttpContextAccessor _accessor;

    public AuthService(IApiClient api, IHttpContextAccessor accessor)
    {
        _api = api;
        _accessor = accessor;
    }

    public async Task<ApiResponse<LoginResponseDto>?> LoginAsync(LoginRequestDto request)
    {
        var response = await _api.PostAsync<LoginRequestDto, LoginResponseDto>("api/auth/login", request);

        if (response is { Success: true, Data: not null })
        {
            var session = _accessor.HttpContext?.Session;
            if (session != null)
            {
                session.SetString(SessionKeys.ApiToken, response.Data.Token);
                session.SetString(SessionKeys.ApiTokenExpiry, response.Data.TokenExpiry.ToString("o"));
                session.SetInt32(SessionKeys.UserId, response.Data.UserId);
                session.SetString(SessionKeys.Username, response.Data.Username);
                session.SetString(SessionKeys.FullName, response.Data.FullName);
                session.SetString(SessionKeys.RoleName, response.Data.RoleName);
            }
        }

        return response;
    }

    public async Task<ApiResponse<object>?> LogoutAsync()
    {
        var session = _accessor.HttpContext?.Session;
        var token = session?.GetString(SessionKeys.ApiToken);

        if (!string.IsNullOrEmpty(token))
        {
            await _api.PostAsync<LogoutRequestDto, object>("api/auth/logout", new LogoutRequestDto { Token = token });
        }

        session?.Clear();
        return ApiResponse<object>.Ok(null, "Logged out successfully.");
    }

    public async Task<ApiResponse<object>?> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        return await _api.PostAsync<ForgotPasswordRequestDto, object>("api/auth/forgot-password", request);
    }

    public async Task<ApiResponse<object>?> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        return await _api.PostAsync<ResetPasswordRequestDto, object>("api/auth/reset-password", request);
    }

    public async Task<ApiResponse<object>?> ChangePasswordAsync(ChangePasswordRequestDto request)
    {
        return await _api.PostAsync<ChangePasswordRequestDto, object>("api/auth/change-password", request);
    }

    public Task<ApiResponse<List<SessionInfoDto>>?> GetSessionsAsync() =>
        _api.GetAsync<List<SessionInfoDto>>("api/auth/sessions");

    public Task<ApiResponse<object>?> RevokeSessionAsync(int sessionTokenId) =>
        _api.DeleteAsync($"api/auth/sessions/{sessionTokenId}");

    public Task<ApiResponse<object>?> RevokeOtherSessionsAsync() =>
        _api.DeleteAsync("api/auth/sessions/others");
}
