using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Models;
using MyApp.Api.Repository.Interfaces;
using MyApp.Api.Security;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogRepository _auditLog;

    public UserService(IUserRepository users, IPasswordHasher passwordHasher, IAuditLogRepository auditLog)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _auditLog = auditLog;
    }

    public async Task<ApiResponse<List<UserListItemDto>>> SearchAsync(UserSearchRequest request)
    {
        var result = await _users.SearchAsync(request);
        var pagination = new PaginationMeta
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = result.TotalCount
        };
        return ApiResponse<List<UserListItemDto>>.Ok(result.Items, pagination: pagination);
    }

    public async Task<ApiResponse<User?>> GetByIdAsync(int id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null)
            return ApiResponse<User?>.Fail("User not found.");

        return ApiResponse<User?>.Ok(user);
    }

    public async Task<ApiResponse<int>> CreateAsync(CreateUserRequest request, int currentUserId)
    {
        if (request.Password != request.ConfirmPassword)
            return ApiResponse<int>.Fail("Passwords do not match.");

        var (hash, salt) = _passwordHasher.Hash(request.Password);
        var newId = await _users.CreateAsync(request, hash, salt, currentUserId);

        await _auditLog.InsertAsync(currentUserId, "Users", "Create", null, $"Created user '{request.Username}' (ID={newId})", null, null);

        return ApiResponse<int>.Ok(newId, "User created successfully.");
    }

    public async Task<ApiResponse<object>> UpdateAsync(int id, UpdateUserRequest request, int currentUserId)
    {
        var existing = await _users.GetByIdAsync(id);
        if (existing is null)
            return ApiResponse<object>.Fail("User not found.");

        await _users.UpdateAsync(id, request, currentUserId);
        await _auditLog.InsertAsync(currentUserId, "Users", "Update", $"FullName={existing.FullName}, Email={existing.Email}", $"FullName={request.FullName}, Email={request.Email}", null, null);

        return ApiResponse<object>.Ok(null, "User updated successfully.");
    }

    public async Task<ApiResponse<object>> DeleteAsync(int id, int currentUserId)
    {
        if (id == currentUserId)
            return ApiResponse<object>.Fail("You cannot delete your own account.");

        await _users.DeleteAsync(id, currentUserId);
        await _auditLog.InsertAsync(currentUserId, "Users", "Delete", $"UserId={id}", "Deleted", null, null);

        return ApiResponse<object>.Ok(null, "User deleted successfully.");
    }

    public async Task<ApiResponse<object>> SetStatusAsync(int id, byte status, int currentUserId)
    {
        if (id == currentUserId && status == 0)
            return ApiResponse<object>.Fail("You cannot deactivate your own account.");

        await _users.SetStatusAsync(id, status, currentUserId);
        await _auditLog.InsertAsync(currentUserId, "Users", "SetStatus", null, $"Status changed to {status} for UserId={id}", null, null);

        return ApiResponse<object>.Ok(null, status == 1 ? "User activated." : "User deactivated.");
    }

    public async Task<ApiResponse<UserPreferences?>> GetPreferencesAsync(int userId)
    {
        var prefs = await _users.GetPreferencesAsync(userId);
        return ApiResponse<UserPreferences?>.Ok(prefs ?? new UserPreferences { UserId = userId });
    }

    public async Task<ApiResponse<object>> SavePreferencesAsync(int userId, UserPreferences preferences)
    {
        preferences.UserId = userId;
        await _users.SavePreferencesAsync(userId, preferences);
        return ApiResponse<object>.Ok(null, "Preferences saved successfully.");
    }
}
