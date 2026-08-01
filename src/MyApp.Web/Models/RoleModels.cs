namespace MyApp.Web.Models;

public record RoleItem(int RoleId, string RoleName, string? Description, bool IsSystemRole, bool IsActive, int UserCount);

public class SaveRoleModel
{
    public int? RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CloneRoleModel
{
    public int SourceRoleId { get; set; }
    public string NewRoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
