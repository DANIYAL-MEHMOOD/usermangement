namespace MyApp.Api.Models;

public class User
{
    public int UserId { get; set; }
    public string? EmployeeNumber { get; set; }
    public string Username { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = [];
    public byte[] PasswordSalt { get; set; } = [];
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? ProfilePicturePath { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public byte Status { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? PasswordExpiryDate { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? LastLogin { get; set; }
    public int LoginCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? CreatedBy { get; set; }
    public int? ModifiedBy { get; set; }
}
