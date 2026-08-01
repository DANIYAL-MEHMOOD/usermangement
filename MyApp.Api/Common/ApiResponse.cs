namespace MyApp.Api.Common;

/// <summary>Standard envelope every API endpoint returns: status, message, data, errors, pagination.</summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = [];
    public PaginationMeta? Pagination { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success", PaginationMeta? pagination = null) =>
        new() { Success = true, Message = message, Data = data, Pagination = pagination };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? [] };
}

public class PaginationMeta
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
