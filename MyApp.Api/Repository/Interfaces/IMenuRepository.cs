using MyApp.Api.Models;

namespace MyApp.Api.Repository.Interfaces;

public interface IMenuRepository
{
    Task<List<Menu>> GetAllAsync();
    Task<List<Menu>> GetForUserAsync(int userId, int roleId);
    Task<int> SaveAsync(int? menuId, int? parentMenuId, string menuName, string? menuIcon,
        string? controllerPage, string? route, int menuOrder, bool status, int userId);
    Task DeleteAsync(int menuId, int modifiedBy);
}
