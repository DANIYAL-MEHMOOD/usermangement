namespace MyApp.Api.DTOs;

/// <summary>
/// Menu tree node returned to the Web layer. Serialized with Title/Icon/
/// DisplayOrder/IsActive so the MVC client never needs to know the internal
/// column names (MenuName/MenuIcon/MenuOrder/Status).
/// </summary>
public class MenuNode
{
    public int MenuId { get; set; }
    public int? ParentMenuId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? ControllerPage { get; set; }
    public string? Route { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public List<MenuNode> Children { get; set; } = [];
}

/// <summary>
/// Menu create/update payload. Field names match the MVC client's
/// MenuNodeDto (Title/Icon/DisplayOrder/IsActive).
/// </summary>
public class SaveMenuRequest
{
    public int? MenuId { get; set; }
    public int? ParentMenuId { get; set; }
    public required string Title { get; set; }
    public string? Icon { get; set; }
    public string? ControllerPage { get; set; }
    public string? Route { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
