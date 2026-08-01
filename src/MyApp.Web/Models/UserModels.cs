namespace MyApp.Web.Models;

public record UserListItem(int UserId, string? EmployeeNumber, string Username, string FullName, string Email,
    string? Phone, string? Designation, string? Department, string? ProfilePicturePath,
    int RoleId, string RoleName, byte Status, DateTime? LastLogin, int LoginCount, DateTime CreatedDate);

public record UserDetail(int UserId, string? EmployeeNumber, string Username, string FullName, string Email,
    string? Phone, string? Designation, string? Department, string? ProfilePicturePath,
    int RoleId, string RoleName, byte Status, DateTime? PasswordExpiryDate, bool MustChangePassword,
    bool IsLocked, DateTime? LastLogin, int LoginCount, DateTime CreatedDate, DateTime? ModifiedDate);

public class CreateUserModel
{
    public string? EmployeeNumber { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public int RoleId { get; set; }
}

public class UpdateUserModel
{
    public string? EmployeeNumber { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public int RoleId { get; set; }
}
