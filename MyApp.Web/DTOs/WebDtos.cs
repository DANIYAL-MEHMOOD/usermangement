namespace MyApp.Web.DTOs;

// Auth DTOs
public class LoginRequestDto
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public bool RememberMe { get; set; }
}

public record LoginResponseDto(
    string Token, DateTime TokenExpiry,
    int UserId, string Username, string FullName, string RoleName, bool MustChangePassword);

public class LogoutRequestDto
{
    public required string Token { get; set; }
}

public class ForgotPasswordRequestDto
{
    public required string Email { get; set; }
}

public class ResetPasswordRequestDto
{
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmNewPassword { get; set; }
}

public class ChangePasswordRequestDto
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmNewPassword { get; set; }
}

/// <summary>One active API session for the current user (Session Management module).</summary>
public record SessionInfoDto(
    int SessionTokenId, DateTime CreatedDate, DateTime ExpiryDate,
    string? IpAddress, string? UserAgent, bool IsCurrent);

// User DTOs
public record UserListItemDto(
    int UserId, string? EmployeeNumber, string Username, string FullName, string Email,
    string? Phone, string? Designation, string? Department, string? ProfilePicturePath,
    int RoleId, string RoleName, byte Status, DateTime? LastLogin, int LoginCount, DateTime CreatedDate);

public record UserDetailDto(
    int UserId, string? EmployeeNumber, string Username, string FullName, string Email,
    string? Phone, string? Designation, string? Department, string? ProfilePicturePath,
    int RoleId, string RoleName, byte Status, DateTime? PasswordExpiryDate, bool MustChangePassword,
    bool IsLocked, DateTime? LastLogin, int LoginCount, DateTime CreatedDate, DateTime? ModifiedDate);

public class CreateUserRequestDto
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

public class UpdateUserRequestDto
{
    public string? EmployeeNumber { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public required int RoleId { get; set; }
}

public class UserSearchRequestDto
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

// Role DTOs
public record RoleItemDto(int RoleId, string RoleName, string? Description, bool IsSystemRole, bool IsActive, int UserCount);

public class SaveRoleRequestDto
{
    public int? RoleId { get; set; }
    public required string RoleName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CloneRoleRequestDto
{
    public required int SourceRoleId { get; set; }
    public required string NewRoleName { get; set; }
    public string? Description { get; set; }
}

// Menu DTOs
public record MenuNodeDto(
    int MenuId, int? ParentMenuId, string Title, string? Icon,
    string? ControllerPage, string? Route, int DisplayOrder, bool IsActive, List<MenuNodeDto> Children);

public class SaveMenuRequestDto
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

// Permission DTOs
public record PermissionTypeDto(int PermissionTypeId, string Code, string Name);

public record RolePermissionRowDto(int MenuId, string MenuName, int? ParentMenuId,
    int PermissionTypeId, string Code, bool Granted);

public class AssignPermissionsRequestDto
{
    public required int RoleId { get; set; }
    public required List<PermissionCellDto> Permissions { get; set; }
}

public record PermissionCellDto(int MenuId, int PermissionTypeId);

// Profile DTOs
public class ProfileDto
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

public class UpdateProfileDto
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? ProfilePicturePath { get; set; }
}

public class UpdatePreferencesDto
{
    public string Theme { get; set; } = "light";
    public bool SidebarCollapsed { get; set; }
    public string Language { get; set; } = "en";
    public string? DashboardLayout { get; set; }
    public string? LandingPage { get; set; }
}

// Dashboard DTOs
public class DashboardSummaryDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int TotalRoles { get; set; }
    public int TotalMenus { get; set; }
    public List<RecentActivityDto> RecentLogins { get; set; } = [];
    public List<RecentActivityDto> AuditLogs { get; set; } = [];
}

public record RecentActivityDto(string Module, string Action, string Details, DateTime Timestamp, string? User = null);

// Audit DTOs
public record AuditLogItemDto(
    long AuditLogId, int? UserId, string? Username, string Module,
    string Action, string? OldValue, string? NewValue, string? Browser,
    string? IPAddress, DateTime CreatedDate);

public class AuditLogSearchRequestDto
{
    public string? SearchTerm { get; set; }
    public string? Module { get; set; }
    public string? Action { get; set; }
    public int? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
