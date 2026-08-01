namespace MyApp.Web.Models;

public record PermissionType(int PermissionTypeId, string Code, string Name);

public record PermissionMatrixCell(int MenuId, string MenuName, int? ParentMenuId,
    int PermissionTypeId, string Code, bool Granted);

public record PermissionCell(int MenuId, int PermissionTypeId);

public class AssignPermissionsModel
{
    public int RoleId { get; set; }
    public List<PermissionCell> Permissions { get; set; } = [];
}
