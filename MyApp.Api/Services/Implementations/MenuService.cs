using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Repository.Interfaces;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

public class MenuService : IMenuService
{
    private readonly IMenuRepository _menus;
    private readonly IAuditLogRepository _auditLog;

    public MenuService(IMenuRepository menus, IAuditLogRepository auditLog)
    {
        _menus = menus;
        _auditLog = auditLog;
    }

    public async Task<ApiResponse<List<MenuNode>>> GetHierarchyAsync()
    {
        var items = await _menus.GetHierarchyAsync();
        return ApiResponse<List<MenuNode>>.Ok(items);
    }

    public async Task<ApiResponse<List<MenuNode>>> GetUserMenusAsync(int userId)
    {
        var items = await _menus.GetUserMenusAsync(userId);
        return ApiResponse<List<MenuNode>>.Ok(items);
    }

    public async Task<ApiResponse<int>> SaveAsync(SaveMenuRequest request, int currentUserId)
    {
        var newId = await _menus.SaveAsync(request, currentUserId);
        await _auditLog.InsertAsync(currentUserId, "Menus", request.MenuId.HasValue ? "Update" : "Create",
            null, $"Menu '{request.Title}' saved (ID={newId})", null, null);

        return ApiResponse<int>.Ok(newId, "Menu saved successfully.");
    }

    public async Task<ApiResponse<object>> DeleteAsync(int id, int currentUserId)
    {
        await _menus.DeleteAsync(id, currentUserId);
        await _auditLog.InsertAsync(currentUserId, "Menus", "Delete", $"MenuId={id}", "Deleted", null, null);

        return ApiResponse<object>.Ok(null, "Menu deleted successfully.");
    }
}
