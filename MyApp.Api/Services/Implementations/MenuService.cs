using System.Data;
using Microsoft.Data.SqlClient;
using MyApp.Api.Common;
using MyApp.Api.Data;
using MyApp.Api.DTOs;
using MyApp.Api.Models;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

/// <summary>
/// Menu management: parent/child hierarchy (unlimited nesting), ordering,
/// icons, role-based menus and CRUD. Database access via stored procedures.
/// </summary>
public class MenuService : IMenuService
{
    private readonly SqlDataAccess _db;

    public MenuService(SqlDataAccess db)
    {
        _db = db;
    }

    public async Task<ApiResponse<List<MenuNode>>> GetHierarchyAsync()
    {
        var flat = await GetMenusAsync();
        return ApiResponse<List<MenuNode>>.Ok(BuildTree(flat));
    }

    public async Task<ApiResponse<List<MenuNode>>> GetUserMenusAsync(int userId, int roleId)
    {
        var flat = await _db.ExecuteReaderAsync("dbo.sp_GetUserMenus", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@RoleId", roleId);
        }, MapMenu);

        return ApiResponse<List<MenuNode>>.Ok(BuildTree(flat));
    }

    public async Task<ApiResponse<int>> SaveAsync(SaveMenuRequest request, int currentUserId)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return ApiResponse<int>.Fail("Menu title is required.");

        var newId = await SaveMenuAsync(request, currentUserId);
        await LogAuditAsync(currentUserId, "Menus", request.MenuId.HasValue ? "Update" : "Create",
            null, $"Menu '{request.Title}' saved (ID={newId})", null, null);

        return ApiResponse<int>.Ok(newId, "Menu saved successfully.");
    }

    public async Task<ApiResponse<object>> DeleteAsync(int id, int currentUserId)
    {
        await _db.ExecuteNonQueryAsync("dbo.sp_DeleteMenu", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@MenuId", id);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", currentUserId);
        });

        await LogAuditAsync(currentUserId, "Menus", "Delete", $"MenuId={id}", "Deleted", null, null);

        return ApiResponse<object>.Ok(null, "Menu deleted successfully.");
    }

    // ------------------------------------------------------------------
    // Database access (stored procedures only)
    // ------------------------------------------------------------------

    private async Task<List<MenuNode>> GetMenusAsync() =>
        await _db.ExecuteReaderAsync("dbo.sp_GetMenus", _ => { }, MapMenu);

    private async Task<int> SaveMenuAsync(SaveMenuRequest request, int userId)
    {
        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_SaveMenu");

        SqlDataAccess.AddParam(command, "@MenuId", request.MenuId);
        SqlDataAccess.AddParam(command, "@ParentMenuId", request.ParentMenuId);
        SqlDataAccess.AddParam(command, "@MenuName", request.Title);
        SqlDataAccess.AddParam(command, "@MenuIcon", request.Icon);
        SqlDataAccess.AddParam(command, "@ControllerPage", request.ControllerPage);
        SqlDataAccess.AddParam(command, "@Route", request.Route);
        SqlDataAccess.AddParam(command, "@MenuOrder", request.DisplayOrder);
        SqlDataAccess.AddParam(command, "@Status", request.IsActive);
        SqlDataAccess.AddParam(command, "@UserId", userId);
        var output = SqlDataAccess.AddOutputParam(command, "@NewMenuId", SqlDbType.Int);

        await command.ExecuteNonQueryAsync();
        return (int)output.Value;
    }

    private static MenuNode MapMenu(SqlDataReader reader) => new()
    {
        MenuId = reader.GetInt32(reader.GetOrdinal("MenuId")),
        ParentMenuId = reader.GetNullableInt32("ParentMenuId"),
        Title = reader.GetString(reader.GetOrdinal("MenuName")),
        Icon = reader.GetNullableString("MenuIcon"),
        ControllerPage = reader.GetNullableString("ControllerPage"),
        Route = reader.GetNullableString("Route"),
        DisplayOrder = reader.GetInt32(reader.GetOrdinal("MenuOrder")),
        IsActive = reader.HasColumn("Status") && Convert.ToBoolean(reader["Status"]),
        Children = []
    };

    /// <summary>Converts the flat, already-ordered result set into a parent/child tree.</summary>
    private static List<MenuNode> BuildTree(List<MenuNode> flat)
    {
        var byId = flat.ToDictionary(m => m.MenuId);
        var roots = new List<MenuNode>();

        foreach (var menu in flat)
        {
            if (menu.ParentMenuId is int parentId && byId.TryGetValue(parentId, out var parent))
                parent.Children.Add(menu);
            else
                roots.Add(menu);
        }

        return roots;
    }

    private async Task LogAuditAsync(int? userId, string module, string action, string? oldValue, string? newValue, string? browser, string? ipAddress) =>
        await _db.ExecuteNonQueryAsync("dbo.sp_InsertAuditLog", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@Module", module);
            SqlDataAccess.AddParam(cmd, "@Action", action);
            SqlDataAccess.AddParam(cmd, "@OldValue", oldValue);
            SqlDataAccess.AddParam(cmd, "@NewValue", newValue);
            SqlDataAccess.AddParam(cmd, "@Browser", browser);
            SqlDataAccess.AddParam(cmd, "@IPAddress", ipAddress);
        });
}
