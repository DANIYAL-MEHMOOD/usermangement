using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Models;

namespace MyApp.Api.Services.Interfaces;

public interface IProfileService
{
    Task<ApiResponse<ProfileDto?>> GetProfileAsync(int userId);
    Task<ApiResponse<object>> UpdateProfileAsync(int userId, UpdateProfileDto request, int currentUserId);
    Task<ApiResponse<UserPreferences?>> GetPreferencesAsync(int userId);
    Task<ApiResponse<object>> UpdatePreferencesAsync(int userId, UpdatePreferencesDto request);
}
