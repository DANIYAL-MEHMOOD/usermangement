using MyApp.Api.Common;
using MyApp.Api.DTOs;

namespace MyApp.Api.Services.Interfaces;

public interface IMenuService
{
    Task<ApiResponse<List<MenuNode>>> GetHierarchyAsync();
    Task<ApiResponse<List<MenuNode>>> GetUserMenusAsync(int userId);
    Task<ApiResponse<int>> SaveAsync(SaveMenuRequest request, int currentUserId);
    Task<ApiResponse<object>> DeleteAsync(int id, int currentUserId);
}
