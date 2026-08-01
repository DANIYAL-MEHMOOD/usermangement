using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Web.DTOs;
using MyApp.Web.Services.Interfaces;
using MyApp.Web.ViewModels;

namespace MyApp.Web.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly ICurrentUserService _currentUser;

    public UsersController(IUserService userService, IRoleService roleService, ICurrentUserService currentUser)
    {
        _userService = userService;
        _roleService = roleService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(UserSearchRequestDto? search = null)
    {
        search ??= new UserSearchRequestDto();

        var usersResponse = await _userService.SearchAsync(search);
        var rolesResponse = await _roleService.GetAllAsync(true);

        var model = new UserListViewModel
        {
            Users = usersResponse is { Success: true, Data: not null } ? usersResponse.Data : [],
            Roles = rolesResponse is { Success: true, Data: not null } ? rolesResponse.Data : [],
            Search = search,
            Pagination = usersResponse?.Pagination,
            IsAdministrator = _currentUser.IsAdministrator
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var rolesResponse = await _roleService.GetAllAsync(true);

        var model = new UserFormViewModel
        {
            AvailableRoles = rolesResponse is { Success: true, Data: not null } ? rolesResponse.Data : []
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var rolesResponse = await _roleService.GetAllAsync(true);
            model.AvailableRoles = rolesResponse is { Success: true, Data: not null } ? rolesResponse.Data : [];
            return View(model);
        }

        var dto = new CreateUserRequestDto
        {
            EmployeeNumber = model.EmployeeNumber,
            Username = model.Username,
            Password = model.Password ?? "",
            ConfirmPassword = model.ConfirmPassword ?? "",
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            Designation = model.Designation,
            Department = model.Department,
            RoleId = model.RoleId
        };

        var response = await _userService.CreateAsync(dto);
        if (response is { Success: true })
        {
            TempData["SuccessMessage"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", response?.Message ?? "Failed to create user.");
        var roles = await _roleService.GetAllAsync(true);
        model.AvailableRoles = roles is { Success: true, Data: not null } ? roles.Data : [];
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userResponse = await _userService.GetByIdAsync(id);
        var rolesResponse = await _roleService.GetAllAsync(true);

        if (userResponse is null || !userResponse.Success || userResponse.Data is null)
        {
            return NotFound();
        }

        var u = userResponse.Data;
        var model = new UserFormViewModel
        {
            UserId = u.UserId,
            EmployeeNumber = u.EmployeeNumber,
            Username = u.Username,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            Designation = u.Designation,
            Department = u.Department,
            RoleId = u.RoleId,
            AvailableRoles = rolesResponse is { Success: true, Data: not null } ? rolesResponse.Data : []
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserFormViewModel model)
    {
        var dto = new UpdateUserRequestDto
        {
            EmployeeNumber = model.EmployeeNumber,
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            Designation = model.Designation,
            Department = model.Department,
            RoleId = model.RoleId
        };

        var response = await _userService.UpdateAsync(id, dto);
        if (response is { Success: true })
        {
            TempData["SuccessMessage"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", response?.Message ?? "Failed to update user.");
        var roles = await _roleService.GetAllAsync(true);
        model.AvailableRoles = roles is { Success: true, Data: not null } ? roles.Data : [];
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _userService.DeleteAsync(id);
        if (response is { Success: true })
        {
            TempData["SuccessMessage"] = "User deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = response?.Message ?? "Failed to delete user.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, bool activate)
    {
        var response = activate ? await _userService.ActivateAsync(id) : await _userService.DeactivateAsync(id);
        if (response is { Success: true })
        {
            TempData["SuccessMessage"] = activate ? "User activated." : "User deactivated.";
        }
        else
        {
            TempData["ErrorMessage"] = response?.Message ?? "Failed to update user status.";
        }
        return RedirectToAction(nameof(Index));
    }
}
