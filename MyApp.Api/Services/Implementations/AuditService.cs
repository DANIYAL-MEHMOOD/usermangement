using MyApp.Api.Common;
using MyApp.Api.Data;
using MyApp.Api.DTOs;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

/// <summary>
/// Audit log search (user, module, action, date range, pagination) and the
/// audit insert used by other services.
/// </summary>
public class AuditService : IAuditService
{
    private readonly SqlDataAccess _db;

    public AuditService(SqlDataAccess db)
    {
        _db = db;
    }

    public async Task<ApiResponse<List<AuditLogItem>>> SearchAsync(AuditLogSearchRequest request)
    {
        var result = await SearchLogsAsync(request);
        var pagination = new PaginationMeta
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = result.TotalCount
        };
        return ApiResponse<List<AuditLogItem>>.Ok(result.Items, pagination: pagination);
    }

    public async Task<ApiResponse<object>> LogAsync(int? userId, string module, string action, string? oldVal, string? newVal, string? browser, string? ip)
    {
        await _db.ExecuteNonQueryAsync("dbo.sp_InsertAuditLog", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@Module", module);
            SqlDataAccess.AddParam(cmd, "@Action", action);
            SqlDataAccess.AddParam(cmd, "@OldValue", oldVal);
            SqlDataAccess.AddParam(cmd, "@NewValue", newVal);
            SqlDataAccess.AddParam(cmd, "@Browser", browser);
            SqlDataAccess.AddParam(cmd, "@IPAddress", ip);
        });
        return ApiResponse<object>.Ok(null, "Audit log created.");
    }

    // ------------------------------------------------------------------
    // Database access (stored procedures only)
    // ------------------------------------------------------------------

    private async Task<PagedResult<AuditLogItem>> SearchLogsAsync(AuditLogSearchRequest request)
    {
        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_GetAuditLogs");

        SqlDataAccess.AddParam(command, "@SearchTerm", request.SearchTerm);
        SqlDataAccess.AddParam(command, "@Module", request.Module);
        SqlDataAccess.AddParam(command, "@Action", request.Action);
        SqlDataAccess.AddParam(command, "@UserId", request.UserId);
        SqlDataAccess.AddParam(command, "@DateFrom", request.FromDate);
        SqlDataAccess.AddParam(command, "@DateTo", request.ToDate);
        SqlDataAccess.AddParam(command, "@PageNumber", request.PageNumber);
        SqlDataAccess.AddParam(command, "@PageSize", request.PageSize);

        var result = new PagedResult<AuditLogItem>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Items.Add(new AuditLogItem(
                reader.GetInt64(reader.GetOrdinal("AuditLogId")),
                reader.GetNullableInt32("UserId"),
                reader.GetNullableString("FullName"),
                reader.GetString(reader.GetOrdinal("Module")),
                reader.GetString(reader.GetOrdinal("Action")),
                reader.GetNullableString("OldValue"),
                reader.GetNullableString("NewValue"),
                reader.GetNullableString("Browser"),
                reader.GetNullableString("IPAddress"),
                reader.GetDateTime(reader.GetOrdinal("ActionDate"))));
        }

        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
            result.TotalCount = reader.GetInt32(0);
        }

        return result;
    }
}
