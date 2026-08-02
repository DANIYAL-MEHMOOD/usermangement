namespace MyApp.Api.Common;

/// <summary>
/// Standard envelope every API endpoint returns: status, message, data,
/// errors, pagination and trace id. Controllers and middleware never return
/// anything that is not wrapped in this envelope.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; } = 200;
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = [];
    public PaginationMeta? Pagination { get; set; }
    public string? TraceId { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success", PaginationMeta? pagination = null) =>
        new() { Success = true, StatusCode = 200, Message = message, Data = data, Pagination = pagination };

    public static ApiResponse<T> Fail(string message, int statusCode = 400, List<string>? errors = null) =>
        new() { Success = false, StatusCode = statusCode, Message = message, Errors = errors ?? [message] };
}

public class PaginationMeta
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
