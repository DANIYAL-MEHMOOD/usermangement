namespace MyApp.Web.Services;

/// <summary>Every Razor Pages request to the backend goes through this client —
/// the Web project never talks to SQL Server directly. Every method returns the
/// API's ApiEnvelope&lt;T&gt; shape so pages can check Success/Message/Errors uniformly.</summary>
public interface IApiClient
{
    Task<ApiEnvelope<T>?> GetAsync<T>(string path, CancellationToken ct = default);
    Task<ApiEnvelope<TResponse>?> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default);
    Task<ApiEnvelope<TResponse>?> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default);
    Task<ApiEnvelope<TResponse>?> PatchAsync<TResponse>(string path, CancellationToken ct = default);
    Task<ApiEnvelope<object>?> DeleteAsync(string path, CancellationToken ct = default);
}
