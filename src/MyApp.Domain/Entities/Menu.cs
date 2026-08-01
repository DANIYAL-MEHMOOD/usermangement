namespace MyApp.Domain.Entities;

public class Menu
{
    public int MenuId { get; set; }
    public int? ParentMenuId { get; set; }
    public string MenuName { get; set; } = string.Empty;
    public string? MenuIcon { get; set; }
    public string? ControllerPage { get; set; }
    public string? Route { get; set; }
    public int MenuOrder { get; set; }
    public bool Status { get; set; }
    public List<Menu> Children { get; set; } = [];
}
