using MyApp.Api.Models;

namespace MyApp.Api.Repository.Interfaces;

public interface IRoleRepository
{
    Task<List<Role>> SearchAsync(string? searchTerm, bool? isActive);
    Task<Role?> GetByIdAsync(int roleId);
    Task<int> SaveAsync(int? roleId, string roleName, string? description, bool isActive, int userId);
    Task DeleteAsync(int roleId, int modifiedBy);
    Task<int> CloneAsync(int sourceRoleId, string newRoleName, string? description, int createdBy);
}
