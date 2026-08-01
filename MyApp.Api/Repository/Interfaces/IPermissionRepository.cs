using MyApp.Api.Models;

namespace MyApp.Api.Repository.Interfaces;

public interface IPermissionRepository
{
    Task<List<PermissionType>> GetPermissionTypesAsync();
    Task<List<(int MenuId, int? ParentMenuId, string MenuName, int MenuOrder, int PermissionTypeId, string Code, string Name, bool Granted)>>
        GetRolePermissionsAsync(int roleId);
    Task AssignPermissionsAsync(int roleId, List<(int MenuId, int PermissionTypeId)> permissions, int userId);
    Task<bool> UserHasPermissionAsync(int userId, int roleId, string controllerPage, string permissionCode);
}
