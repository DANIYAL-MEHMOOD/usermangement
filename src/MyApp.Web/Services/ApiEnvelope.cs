namespace MyApp.Web.Services;

/// <summary>Mirrors MyApp.Application.Common.ApiResponse&lt;T&gt; on the API side —
/// duplicated here (rather than referencing the Application project) so the Web
/// project's only line of communication with the backend stays HTTP, never a
/// shared assembly that could tempt someone into calling a repository directly.</summary>
public record ApiEnvelope<T>(bool Success, string Message, T? Data, List<string>? Errors, PaginationMetaDto? Pagination);

public record PaginationMetaDto(int PageNumber, int PageSize, int TotalCount, int TotalPages);
