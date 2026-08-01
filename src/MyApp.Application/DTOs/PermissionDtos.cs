namespace MyApp.Application.DTOs;

public record PermissionMatrixCellDto(int MenuId, string MenuName, int? ParentMenuId,
    int PermissionTypeId, string Code, bool Granted);

public class AssignPermissionsRequest
{
    public required int RoleId { get; set; }
    public required List<PermissionCellRequest> Permissions { get; set; }
}

public record PermissionCellRequest(int MenuId, int PermissionTypeId);
