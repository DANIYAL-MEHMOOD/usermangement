namespace MyApp.Web.Models;

public record MenuItem(int MenuId, int? ParentMenuId, string MenuName, string? MenuIcon,
    string? ControllerPage, string? Route, int MenuOrder, bool Status, List<MenuItem> Children);

public class SaveMenuModel
{
    public int? MenuId { get; set; }
    public int? ParentMenuId { get; set; }
    public string MenuName { get; set; } = string.Empty;
    public string? MenuIcon { get; set; }
    public string? ControllerPage { get; set; }
    public string? Route { get; set; }
    public int MenuOrder { get; set; }
    public bool Status { get; set; } = true;
}
