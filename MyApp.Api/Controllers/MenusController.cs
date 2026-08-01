using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ICurrentUserService _currentUser;

    public MenusController(IMenuService menuService, ICurrentUserService currentUser)
    {
        _menuService = menuService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MenuNode>>>> GetAll()
    {
        var response = await _menuService.GetHierarchyAsync();
        return Ok(response);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<List<MenuNode>>>> GetMine()
    {
        var response = await _menuService.GetUserMenusAsync(_currentUser.UserId);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Save([FromBody] SaveMenuRequest request)
    {
        var response = await _menuService.SaveAsync(request, _currentUser.UserId);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var response = await _menuService.DeleteAsync(id, _currentUser.UserId);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }
}
