using MyApp.Web.Common;

namespace MyApp.Web.ApiClient;

public interface IApiClient
{
    Task<ApiResponse<T>?> GetAsync<T>(string path, CancellationToken ct = default);
    Task<ApiResponse<TResponse>?> PostAsync<TRequest, TResponse>(string path, TRequest? body, CancellationToken ct = default);
    Task<ApiResponse<TResponse>?> PutAsync<TRequest, TResponse>(string path, TRequest? body, CancellationToken ct = default);
    Task<ApiResponse<TResponse>?> PatchAsync<TResponse>(string path, CancellationToken ct = default);
    Task<ApiResponse<TResponse>?> PatchAsync<TRequest, TResponse>(string path, TRequest? body, CancellationToken ct = default);
    Task<ApiResponse<object>?> DeleteAsync(string path, CancellationToken ct = default);
}
