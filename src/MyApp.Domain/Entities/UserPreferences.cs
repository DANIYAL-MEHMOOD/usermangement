namespace MyApp.Domain.Entities;

public class UserPreferences
{
    public int UserId { get; set; }
    public string Theme { get; set; } = "light";
    public bool SidebarCollapsed { get; set; }
    public string Language { get; set; } = "en";
    public string? DashboardLayout { get; set; }
    public string? LandingPage { get; set; }
}
