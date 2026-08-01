using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Common;
using MyApp.Application.Interfaces;

namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _auditLog;
    public AuditLogsController(IAuditLogRepository auditLog) => _auditLog = auditLog;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Search(
        [FromQuery] int? userId, [FromQuery] string? module, [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25)
    {
        var result = await _auditLog.SearchAsync(userId, module, dateFrom, dateTo, pageNumber, pageSize);
        var pagination = new PaginationMeta { PageNumber = pageNumber, PageSize = pageSize, TotalCount = result.TotalCount };
        return Ok(ApiResponse<object>.Ok(result.Items, pagination: pagination));
    }
}
