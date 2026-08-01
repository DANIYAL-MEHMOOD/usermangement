namespace MyApp.Api.DTOs;

public record RoleDto(int RoleId, string RoleName, string? Description, bool IsSystemRole, bool IsActive, int UserCount);

public class SaveRoleRequest
{
    public int? RoleId { get; set; }
    public required string RoleName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CloneRoleRequest
{
    public required int SourceRoleId { get; set; }
    public required string NewRoleName { get; set; }
    public string? Description { get; set; }
}
