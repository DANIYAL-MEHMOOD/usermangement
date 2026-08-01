using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Models;

namespace MyApp.Api.Services.Interfaces;

public interface IUserService
{
    Task<ApiResponse<List<UserListItemDto>>> SearchAsync(UserSearchRequest request);
    Task<ApiResponse<User?>> GetByIdAsync(int id);
    Task<ApiResponse<int>> CreateAsync(CreateUserRequest request, int currentUserId);
    Task<ApiResponse<object>> UpdateAsync(int id, UpdateUserRequest request, int currentUserId);
    Task<ApiResponse<object>> DeleteAsync(int id, int currentUserId);
    Task<ApiResponse<object>> SetStatusAsync(int id, byte status, int currentUserId);
    Task<ApiResponse<UserPreferences?>> GetPreferencesAsync(int userId);
    Task<ApiResponse<object>> SavePreferencesAsync(int userId, UserPreferences preferences);
}
