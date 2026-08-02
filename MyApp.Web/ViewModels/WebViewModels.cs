using MyApp.Web.Common;
using MyApp.Web.DTOs;

namespace MyApp.Web.ViewModels;

public class LoginViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ForgotPasswordViewModel
{
    public string Email { get; set; } = string.Empty;
    public string? Message { get; set; }
    public bool Success { get; set; }
}

public class ResetPasswordViewModel
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
    public string? Message { get; set; }
    public bool Success { get; set; }
}

public class ChangePasswordViewModel
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
    public string? Message { get; set; }
    public bool Success { get; set; }
}

public class DashboardViewModel
{
    public DashboardSummaryDto Summary { get; set; } = new();
}

public class UserListViewModel
{
    public List<UserListItemDto> Users { get; set; } = [];
    public List<RoleItemDto> Roles { get; set; } = [];
    public UserSearchRequestDto Search { get; set; } = new();
    public PaginationMeta? Pagination { get; set; }
    public bool IsAdministrator { get; set; }
}

public class UserFormViewModel
{
    public int? UserId { get; set; }
    public string? EmployeeNumber { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public int RoleId { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public List<RoleItemDto> AvailableRoles { get; set; } = [];
}

public class RoleListViewModel
{
    public List<RoleItemDto> Roles { get; set; } = [];
}

public class RoleFormViewModel
{
    public int? RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RoleCloneViewModel
{
    public int SourceRoleId { get; set; }
    public string NewRoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<RoleItemDto> ExistingRoles { get; set; } = [];
}

public class MenuListViewModel
{
    public List<MenuNodeDto> Menus { get; set; } = [];
}

public class MenuFormViewModel
{
    public int? MenuId { get; set; }
    public int? ParentMenuId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? ControllerPage { get; set; }
    public string? Route { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<MenuNodeDto> ParentOptions { get; set; } = [];
}

public class PermissionMatrixViewModel
{
    public List<RoleItemDto> Roles { get; set; } = [];
    public int SelectedRoleId { get; set; }
    public string SelectedRoleName { get; set; } = string.Empty;
    public List<PermissionTypeDto> PermissionTypes { get; set; } = [];
    public List<RolePermissionRowDto> Matrix { get; set; } = [];
}

public class ProfileViewModel
{
    public ProfileDto Profile { get; set; } = new();
    public List<SessionInfoDto> Sessions { get; set; } = [];
    public string? Message { get; set; }
}

public class AdministrationViewModel
{
    public string ActiveTab { get; set; } = "users";
    public List<UserListItemDto> RecentUsers { get; set; } = [];
    public List<RoleItemDto> Roles { get; set; } = [];
    public List<MenuNodeDto> Menus { get; set; } = [];
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "en";
    public bool SidebarCollapsed { get; set; }
}

public class AuditLogListViewModel
{
    public List<AuditLogItemDto> Items { get; set; } = [];
    public AuditLogSearchRequestDto Search { get; set; } = new();
    public PaginationMeta? Pagination { get; set; }
}

public class SettingsViewModel
{
    public string Theme { get; set; } = "light";
    public bool SidebarCollapsed { get; set; }
    public string Language { get; set; } = "en";
    public string? Message { get; set; }
}

public class ErrorViewModel
{
    public string RequestId { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Message { get; set; } = "An unexpected error occurred.";
    public int StatusCode { get; set; } = 500;
}
