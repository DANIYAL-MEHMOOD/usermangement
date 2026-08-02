namespace MyApp.Web.Middlewares;

public class GlobalExceptionMiddleware
{
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
            var requestId = context.TraceIdentifier;
            var refNumber = $"REF-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            _logger.LogError(ex, "Unhandled Web exception. RequestId={RequestId}, ReferenceNumber={ReferenceNumber}", requestId, refNumber);

            if (context.Request.Headers.XRequestedWith == "XMLHttpRequest" ||
                context.Request.Headers.Accept.ToString().Contains("application/json"))
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 500;
                var json = $"{{\"success\":false,\"statusCode\":500,\"message\":\"An unexpected error occurred. Please contact support referencing {refNumber}.\",\"traceId\":\"{requestId}\"}}";
                await context.Response.WriteAsync(json);
                return;
            }

            context.Response.Redirect($"/Error?requestId={Uri.EscapeDataString(requestId)}&referenceNumber={Uri.EscapeDataString(refNumber)}");
        }
    }
}
