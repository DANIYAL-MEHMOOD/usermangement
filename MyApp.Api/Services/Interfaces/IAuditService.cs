using MyApp.Api.Common;
using MyApp.Api.DTOs;

namespace MyApp.Api.Services.Interfaces;

public interface IAuditService
{
    Task<ApiResponse<List<AuditLogItem>>> SearchAsync(AuditLogSearchRequest request);
    Task<ApiResponse<object>> LogAsync(int? userId, string module, string action, string? oldVal, string? newVal, string? browser, string? ip);
}
