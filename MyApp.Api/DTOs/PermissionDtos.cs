namespace MyApp.Api.DTOs;

/// <summary>One cell of the role permission matrix: menu x permission-type with granted flag.</summary>
public record RolePermissionRow(int MenuId, string MenuName, int? ParentMenuId,
    int PermissionTypeId, string Code, bool Granted);

public class AssignPermissionsRequest
{
    public required int RoleId { get; set; }
    public required List<PermissionCellRequest> Permissions { get; set; }
}

public record PermissionCellRequest(int MenuId, int PermissionTypeId);
