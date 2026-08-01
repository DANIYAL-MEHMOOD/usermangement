using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Common;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;

namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenusController : ControllerBase
{
    private readonly IMenuRepository _menus;
    private readonly ICurrentUserService _currentUser;

    public MenusController(IMenuRepository menus, ICurrentUserService currentUser)
    {
        _menus = menus;
        _currentUser = currentUser;
    }

    /// <summary>Full flat/tree list for the Menu Management admin screen.</summary>
    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<List<Menu>>>> GetAll()
    {
        var menus = await _menus.GetAllAsync();
        return Ok(ApiResponse<List<Menu>>.Ok(menus));
    }

    /// <summary>The sidebar tree for the currently logged-in user — only menus
    /// their role (or explicit user override) grants VIEW on.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<List<Menu>>>> GetMine([FromServices] IUserRepository users)
    {
        var userId = _currentUser.UserId ?? 0;
        var user = await users.GetByIdAsync(userId);
        if (user is null) return Unauthorized();

        var menus = await _menus.GetForUserAsync(userId, user.RoleId);
        return Ok(ApiResponse<List<Menu>>.Ok(menus));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> Save(SaveMenuRequest request)
    {
        var menuId = await _menus.SaveAsync(request.MenuId, request.ParentMenuId, request.MenuName,
            request.MenuIcon, request.ControllerPage, request.Route, request.MenuOrder, request.Status,
            _currentUser.UserId ?? 0);
        return Ok(ApiResponse<object>.Ok(new { MenuId = menuId }, "Menu saved."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        try
        {
            await _menus.DeleteAsync(id, _currentUser.UserId ?? 0);
            return Ok(ApiResponse<object>.Ok(new { }, "Menu deleted."));
        }
        catch (Exception ex) when (ex.Message.Contains("child menus"))
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
