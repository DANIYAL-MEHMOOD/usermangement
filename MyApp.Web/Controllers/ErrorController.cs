using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Web.ViewModels;

namespace MyApp.Web.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    [Route("Error")]
    [Route("Error/Index")]
    public IActionResult Index(string? requestId = null, string? referenceNumber = null)
    {
        var model = new ErrorViewModel
        {
            RequestId = requestId ?? HttpContext.TraceIdentifier,
            ReferenceNumber = referenceNumber ?? $"REF-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            Message = "An unexpected error occurred while processing your request.",
            StatusCode = 500
        };
        return View(model);
    }

    [Route("Error/{statusCode:int}")]
    public IActionResult StatusCodeHandler(int statusCode)
    {
        if (statusCode == 404)
        {
            return View("404");
        }
        if (statusCode == 403 || statusCode == 401)
        {
            return View("AccessDenied");
        }
        var model = new ErrorViewModel
        {
            RequestId = HttpContext.TraceIdentifier,
            ReferenceNumber = $"REF-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            Message = $"Error {statusCode} occurred.",
            StatusCode = statusCode
        };
        return View("Index", model);
    }

    [Route("Error/AccessDenied")]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
