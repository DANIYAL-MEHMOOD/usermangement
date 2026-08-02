using System.Net;
using System.Text.Json;
using MyApp.Api.Common;
using MyApp.Api.Exceptions;

namespace MyApp.Api.Middlewares;

/// <summary>
/// Catches unhandled exceptions and returns the standard ApiResponse envelope
/// with a friendly message — never leaks stack traces, SQL errors, connection
/// strings or server paths to the client.
/// </summary>
public class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled API exception on {Path}. TraceId={TraceId}", context.Request.Path, context.TraceIdentifier);

            context.Response.ContentType = "application/json";

            int statusCode = (int)HttpStatusCode.InternalServerError;
            string message = "An unexpected error occurred. Please try again or contact support.";
            List<string>? errors = null;

            if (ex is AppException appEx)
            {
                statusCode = appEx.StatusCode;
                message = appEx.Message;
                if (appEx is ValidationException valEx)
                {
                    errors = valEx.Errors.ToList();
                }
            }

            context.Response.StatusCode = statusCode;

            var response = new ApiResponse<object>
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
                Errors = errors ?? [message],
                TraceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }
}
