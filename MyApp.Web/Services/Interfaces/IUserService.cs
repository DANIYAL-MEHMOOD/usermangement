using MyApp.Web.Common;
using MyApp.Web.DTOs;

namespace MyApp.Web.Services.Interfaces;

public interface IUserService
{
    Task<ApiResponse<List<UserListItemDto>>?> SearchAsync(UserSearchRequestDto request);
    Task<ApiResponse<UserDetailDto?>?> GetByIdAsync(int id);
    Task<ApiResponse<int>?> CreateAsync(CreateUserRequestDto request);
    Task<ApiResponse<object>?> UpdateAsync(int id, UpdateUserRequestDto request);
    Task<ApiResponse<object>?> DeleteAsync(int id);
    Task<ApiResponse<object>?> ActivateAsync(int id);
    Task<ApiResponse<object>?> DeactivateAsync(int id);
}

public interface IRoleService
{
    Task<ApiResponse<List<RoleItemDto>>?> GetAllAsync(bool includeSystem = true);
    Task<ApiResponse<RoleItemDto?>?> GetByIdAsync(int id);
    Task<ApiResponse<int>?> SaveAsync(SaveRoleRequestDto request);
    Task<ApiResponse<object>?> DeleteAsync(int id);
    Task<ApiResponse<int>?> CloneAsync(CloneRoleRequestDto request);
    Task<ApiResponse<object>?> AssignRoleAsync(int userId, int roleId);
}
