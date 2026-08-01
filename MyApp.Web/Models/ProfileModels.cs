namespace MyApp.Web.Models;

public class ProfileModel
{
    public int UserId { get; set; }
    public string? EmployeeNumber { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? ProfilePicturePath { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime? LastLogin { get; set; }
    public string Theme { get; set; } = "light";
    public bool SidebarCollapsed { get; set; }
    public string Language { get; set; } = "en";
    public string? DashboardLayout { get; set; }
    public string? LandingPage { get; set; }
}

public class UpdateProfileModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? ProfilePicturePath { get; set; }
}

public class UserPreferencesModel
{
    public string Theme { get; set; } = "light";
    public bool SidebarCollapsed { get; set; }
    public string Language { get; set; } = "en";
    public string? DashboardLayout { get; set; }
    public string? LandingPage { get; set; }
}
