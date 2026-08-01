using MyApp.Api.Common;
using MyApp.Api.DTOs;
using MyApp.Api.Repository.Interfaces;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Services.Implementations;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLog;

    public AuditService(IAuditLogRepository auditLog)
    {
        _auditLog = auditLog;
    }

    public async Task<ApiResponse<List<AuditLogItem>>> SearchAsync(AuditLogSearchRequest request)
    {
        var result = await _auditLog.SearchAsync(request);
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
        await _auditLog.InsertAsync(userId, module, action, oldVal, newVal, browser, ip);
        return ApiResponse<object>.Ok(null, "Audit log created.");
    }
}
