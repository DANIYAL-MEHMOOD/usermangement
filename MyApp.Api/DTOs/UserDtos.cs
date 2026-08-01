namespace MyApp.Api.DTOs;

public record UserListItemDto(
    int UserId, string? EmployeeNumber, string Username, string FullName, string Email,
    string? Phone, string? Designation, string? Department, string? ProfilePicturePath,
    int RoleId, string RoleName, byte Status, DateTime? LastLogin, int LoginCount, DateTime CreatedDate);

public record UserDetailDto(
    int UserId, string? EmployeeNumber, string Username, string FullName, string Email,
    string? Phone, string? Designation, string? Department, string? ProfilePicturePath,
    int RoleId, string RoleName, byte Status, DateTime? PasswordExpiryDate, bool MustChangePassword,
    bool IsLocked, DateTime? LastLogin, int LoginCount, DateTime CreatedDate, DateTime? ModifiedDate);

public class CreateUserRequest
{
    public string? EmployeeNumber { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string ConfirmPassword { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public required int RoleId { get; set; }
}

public class UpdateUserRequest
{
    public string? EmployeeNumber { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public required int RoleId { get; set; }
}

public class ChangePasswordRequest
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmNewPassword { get; set; }
}

public class UserSearchRequest
{
    public string? SearchTerm { get; set; }
    public int? RoleId { get; set; }
    public byte? Status { get; set; }
    public string? Department { get; set; }
    public string SortColumn { get; set; } = "FullName";
    public string SortDirection { get; set; } = "ASC";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
