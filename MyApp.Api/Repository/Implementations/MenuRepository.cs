using System.Data;
using MyApp.Api.Repository.Interfaces;
using MyApp.Api.Security;
using MyApp.Api.Models;
using MyApp.Api.Data;

namespace MyApp.Api.Repository.Implementations;

public class MenuRepository : IMenuRepository
{
    private readonly SqlDataAccess _db;
    public MenuRepository(SqlDataAccess db) => _db = db;

    public async Task<List<Menu>> GetAllAsync()
    {
        var flat = await _db.ExecuteReaderAsync("dbo.sp_GetMenus", _ => { }, MapMenu);
        return BuildTree(flat);
    }

    public async Task<List<Menu>> GetForUserAsync(int userId, int roleId)
    {
        var flat = await _db.ExecuteReaderAsync("dbo.sp_GetUserMenus", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@RoleId", roleId);
        }, MapMenu);
        return BuildTree(flat);
    }

    public async Task<int> SaveAsync(int? menuId, int? parentMenuId, string menuName, string? menuIcon,
        string? controllerPage, string? route, int menuOrder, bool status, int userId)
    {
        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_SaveMenu");

        SqlDataAccess.AddParam(command, "@MenuId", menuId);
        SqlDataAccess.AddParam(command, "@ParentMenuId", parentMenuId);
        SqlDataAccess.AddParam(command, "@MenuName", menuName);
        SqlDataAccess.AddParam(command, "@MenuIcon", menuIcon);
        SqlDataAccess.AddParam(command, "@ControllerPage", controllerPage);
        SqlDataAccess.AddParam(command, "@Route", route);
        SqlDataAccess.AddParam(command, "@MenuOrder", menuOrder);
        SqlDataAccess.AddParam(command, "@Status", status);
        SqlDataAccess.AddParam(command, "@UserId", userId);
        var output = SqlDataAccess.AddOutputParam(command, "@NewMenuId", SqlDbType.Int);

        await command.ExecuteNonQueryAsync();
        return (int)output.Value;
    }

    public Task DeleteAsync(int menuId, int modifiedBy) =>
        _db.ExecuteNonQueryAsync("dbo.sp_DeleteMenu", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@MenuId", menuId);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", modifiedBy);
        });

    private static Menu MapMenu(Microsoft.Data.SqlClient.SqlDataReader reader) => new()
    {
        MenuId = reader.GetInt32(reader.GetOrdinal("MenuId")),
        ParentMenuId = reader.IsDBNull(reader.GetOrdinal("ParentMenuId")) ? null : reader.GetInt32(reader.GetOrdinal("ParentMenuId")),
        MenuName = reader.GetString(reader.GetOrdinal("MenuName")),
        MenuIcon = reader.IsDBNull(reader.GetOrdinal("MenuIcon")) ? null : reader.GetString(reader.GetOrdinal("MenuIcon")),
        ControllerPage = reader.IsDBNull(reader.GetOrdinal("ControllerPage")) ? null : reader.GetString(reader.GetOrdinal("ControllerPage")),
        Route = reader.IsDBNull(reader.GetOrdinal("Route")) ? null : reader.GetString(reader.GetOrdinal("Route")),
        MenuOrder = reader.GetInt32(reader.GetOrdinal("MenuOrder")),
        Status = reader.HasColumn("Status") && Convert.ToBoolean(reader["Status"])
    };

    /// <summary>Converts the flat, already-ordered result set into a parent/child tree for the sidebar.</summary>
    private static List<Menu> BuildTree(List<Menu> flat)
    {
        var byId = flat.ToDictionary(m => m.MenuId);
        var roots = new List<Menu>();

        foreach (var menu in flat)
        {
            if (menu.ParentMenuId is int parentId && byId.TryGetValue(parentId, out var parent))
                parent.Children.Add(menu);
            else
                roots.Add(menu);
        }

        return roots;
    }
}
