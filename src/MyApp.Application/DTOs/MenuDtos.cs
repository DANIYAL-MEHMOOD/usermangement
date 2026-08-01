namespace MyApp.Application.DTOs;

public record MenuDto(int MenuId, int? ParentMenuId, string MenuName, string? MenuIcon,
    string? ControllerPage, string? Route, int MenuOrder, bool Status);

public class SaveMenuRequest
{
    public int? MenuId { get; set; }
    public int? ParentMenuId { get; set; }
    public required string MenuName { get; set; }
    public string? MenuIcon { get; set; }
    public string? ControllerPage { get; set; }
    public string? Route { get; set; }
    public int MenuOrder { get; set; }
    public bool Status { get; set; } = true;
}
