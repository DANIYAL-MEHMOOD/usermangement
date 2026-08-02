using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Web.DTOs;
using MyApp.Web.Filters;
using MyApp.Web.Services.Interfaces;
using MyApp.Web.ViewModels;

namespace MyApp.Web.Controllers;

/// <summary>
/// System audit log browser: user, module, action, old/new value, IP,
/// browser and date/time with filtering and pagination.
/// </summary>
[Authorize]
[ServiceFilter(typeof(AdminOnlyFilter))]
public class AuditLogsController : Controller
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(AuditLogSearchRequestDto? search = null)
    {
        search ??= new AuditLogSearchRequestDto();

        var response = await _auditService.SearchAsync(search);

        var model = new AuditLogListViewModel
        {
            Items = response is { Success: true, Data: not null } ? response.Data : [],
            Search = search,
            Pagination = response?.Pagination
        };

        return View(model);
    }
}
