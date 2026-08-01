using MyApp.Api.Common;
using MyApp.Api.Models;

namespace MyApp.Api.Repository.Interfaces;

public interface IAuditLogRepository
{
    Task InsertAsync(int? userId, string module, string action, string? oldValue, string? newValue,
        string? browser, string? ipAddress);

    Task<PagedResult<AuditLog>> SearchAsync(int? userId, string? module, DateTime? dateFrom,
        DateTime? dateTo, int pageNumber, int pageSize);
}
