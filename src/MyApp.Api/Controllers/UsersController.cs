using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Common;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;

namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogRepository _auditLog;
    private readonly ICurrentUserService _currentUser;

    public UsersController(IUserRepository users, IPasswordHasher passwordHasher,
        IAuditLogRepository auditLog, ICurrentUserService currentUser)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _auditLog = auditLog;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserListItemDto>>>> Search([FromQuery] UserSearchRequest request)
    {
        var result = await _users.SearchAsync(request);
        var pagination = new PaginationMeta { PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = result.TotalCount };
        return Ok(ApiResponse<List<UserListItemDto>>.Ok(result.Items, pagination: pagination));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(int id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null) return NotFound(ApiResponse<object>.Fail("User not found."));

        return Ok(ApiResponse<object>.Ok(new UserDetailDto(
            user.UserId, user.EmployeeNumber, user.Username, user.FullName, user.Email, user.Phone,
            user.Designation, user.Department, user.ProfilePicturePath, user.RoleId, user.RoleName,
            user.Status, user.PasswordExpiryDate, user.MustChangePassword, user.IsLocked,
            user.LastLogin, user.LoginCount, user.CreatedDate, user.ModifiedDate)));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> Create(CreateUserRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            return BadRequest(ApiResponse<object>.Fail("Passwords do not match."));

        var (hash, salt) = _passwordHasher.Hash(request.Password);
        var createdBy = _currentUser.UserId ?? 0;

        try
        {
            var newUserId = await _users.CreateAsync(request, hash, salt, createdBy);
            await _auditLog.InsertAsync(createdBy, "Users", "Create", null, request.Username,
                _currentUser.UserAgent, _currentUser.IpAddress);
            return Ok(ApiResponse<object>.Ok(new { UserId = newUserId }, "User created."));
        }
        catch (Exception ex) when (ex.Message.Contains("already exists"))
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, UpdateUserRequest request)
    {
        var modifiedBy = _currentUser.UserId ?? 0;
        try
        {
            await _users.UpdateAsync(id, request, modifiedBy);
            await _auditLog.InsertAsync(modifiedBy, "Users", "Update", null, request.Email,
                _currentUser.UserAgent, _currentUser.IpAddress);
            return Ok(ApiResponse<object>.Ok(new { }, "User updated."));
        }
        catch (Exception ex) when (ex.Message.Contains("already in use"))
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var modifiedBy = _currentUser.UserId ?? 0;
        await _users.DeleteAsync(id, modifiedBy);
        await _auditLog.InsertAsync(modifiedBy, "Users", "Delete", id.ToString(), null,
            _currentUser.UserAgent, _currentUser.IpAddress);
        return Ok(ApiResponse<object>.Ok(new { }, "User deleted."));
    }

    [HttpPatch("{id:int}/activate")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> Activate(int id)
    {
        await _users.SetStatusAsync(id, 1, _currentUser.UserId ?? 0);
        return Ok(ApiResponse<object>.Ok(new { }, "User activated."));
    }

    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(int id)
    {
        await _users.SetStatusAsync(id, 0, _currentUser.UserId ?? 0);
        return Ok(ApiResponse<object>.Ok(new { }, "User deactivated."));
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<ApiResponse<object>>> GetPreferences()
    {
        var userId = _currentUser.UserId ?? 0;
        var prefs = await _users.GetPreferencesAsync(userId);
        return Ok(ApiResponse<object>.Ok(prefs));
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<ApiResponse<object>>> SavePreferences(MyApp.Domain.Entities.UserPreferences preferences)
    {
        var userId = _currentUser.UserId ?? 0;
        preferences.UserId = userId;
        await _users.SavePreferencesAsync(userId, preferences);
        return Ok(ApiResponse<object>.Ok(new { }, "Preferences saved."));
    }
}
