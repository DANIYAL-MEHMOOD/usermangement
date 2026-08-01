using System.Net;
using System.Text.Json;
using MyApp.Application.Common;
using Serilog;

namespace MyApp.Api.Middleware;

/// <summary>Catches unhandled exceptions and returns the standard ApiResponse envelope
/// with a friendly message — never leaks stack traces to the client.</summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception on {Path}", context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = ApiResponse<object>.Fail("An unexpected error occurred. Please try again or contact support.");
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
