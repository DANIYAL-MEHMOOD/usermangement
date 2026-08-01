using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Models;

namespace MyApp.Api.Services.Interfaces;

public interface IRoleService
{
    Task<ApiResponse<List<RoleItem>>> GetAllAsync(bool includeSystemRoles = true);
    Task<ApiResponse<Role?>> GetByIdAsync(int id);
    Task<ApiResponse<int>> SaveAsync(SaveRoleRequest request, int currentUserId);
    Task<ApiResponse<object>> DeleteAsync(int id, int currentUserId);
    Task<ApiResponse<int>> CloneAsync(CloneRoleRequest request, int currentUserId);
    Task<ApiResponse<object>> AssignRoleAsync(int userId, int roleId, int currentUserId);
}
