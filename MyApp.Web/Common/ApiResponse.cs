namespace MyApp.Web.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public PaginationMeta? Pagination { get; set; }
    public string? TraceId { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null, PaginationMeta? pagination = null) => new()
    {
        Success = true,
        StatusCode = 200,
        Message = message,
        Data = data,
        Pagination = pagination
    };

    public static ApiResponse<T> Fail(string message, int statusCode = 400, List<string>? errors = null) => new()
    {
        Success = false,
        StatusCode = statusCode,
        Message = message,
        Errors = errors ?? [message]
    };
}
