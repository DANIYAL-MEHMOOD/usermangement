using MyApp.Api.Common;
using MyApp.Api.Repository.Interfaces;
using MyApp.Api.Security;
using MyApp.Api.Models;
using MyApp.Api.Data;

namespace MyApp.Api.Repository.Implementations;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly SqlDataAccess _db;
    public AuditLogRepository(SqlDataAccess db) => _db = db;

    public Task InsertAsync(int? userId, string module, string action, string? oldValue, string? newValue,
        string? browser, string? ipAddress) =>
        _db.ExecuteNonQueryAsync("dbo.sp_InsertAuditLog", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@Module", module);
            SqlDataAccess.AddParam(cmd, "@Action", action);
            SqlDataAccess.AddParam(cmd, "@OldValue", oldValue);
            SqlDataAccess.AddParam(cmd, "@NewValue", newValue);
            SqlDataAccess.AddParam(cmd, "@Browser", browser);
            SqlDataAccess.AddParam(cmd, "@IPAddress", ipAddress);
        });

    public async Task<PagedResult<AuditLog>> SearchAsync(int? userId, string? module, DateTime? dateFrom,
        DateTime? dateTo, int pageNumber, int pageSize)
    {
        await using var connection = await _db.OpenConnectionAsync();
        await using var command = SqlDataAccess.CreateCommand(connection, "dbo.sp_GetAuditLogs");

        SqlDataAccess.AddParam(command, "@UserId", userId);
        SqlDataAccess.AddParam(command, "@Module", module);
        SqlDataAccess.AddParam(command, "@DateFrom", dateFrom);
        SqlDataAccess.AddParam(command, "@DateTo", dateTo);
        SqlDataAccess.AddParam(command, "@PageNumber", pageNumber);
        SqlDataAccess.AddParam(command, "@PageSize", pageSize);

        var result = new PagedResult<AuditLog>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Items.Add(new AuditLog
            {
                AuditLogId = reader.GetInt64(reader.GetOrdinal("AuditLogId")),
                UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt32(reader.GetOrdinal("UserId")),
                FullName = reader.IsDBNull(reader.GetOrdinal("FullName")) ? null : reader.GetString(reader.GetOrdinal("FullName")),
                Module = reader.GetString(reader.GetOrdinal("Module")),
                Action = reader.GetString(reader.GetOrdinal("Action")),
                OldValue = reader.IsDBNull(reader.GetOrdinal("OldValue")) ? null : reader.GetString(reader.GetOrdinal("OldValue")),
                NewValue = reader.IsDBNull(reader.GetOrdinal("NewValue")) ? null : reader.GetString(reader.GetOrdinal("NewValue")),
                Browser = reader.IsDBNull(reader.GetOrdinal("Browser")) ? null : reader.GetString(reader.GetOrdinal("Browser")),
                IPAddress = reader.IsDBNull(reader.GetOrdinal("IPAddress")) ? null : reader.GetString(reader.GetOrdinal("IPAddress")),
                ActionDate = reader.GetDateTime(reader.GetOrdinal("ActionDate"))
            });
        }

        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
            result.TotalCount = reader.GetInt32(0);
        }

        return result;
    }
}
