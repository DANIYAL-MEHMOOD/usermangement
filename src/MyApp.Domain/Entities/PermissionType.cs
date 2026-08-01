namespace MyApp.Domain.Entities;

public class PermissionType
{
    public int PermissionTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
