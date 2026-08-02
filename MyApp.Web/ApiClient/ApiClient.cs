using System.Net.Http.Json;
using System.Text.Json;
using MyApp.Web.Common;

namespace MyApp.Web.ApiClient;

/// <summary>
/// Typed HTTP client the Web project uses for every call to MyApp.Api.
/// The opaque session token is read from the server-side session and forwarded
/// on the X-Api-Token header. No JWT, no refresh-token exchange, and the token
/// is never exposed to browser JavaScript.
/// </summary>
public class ApiClient : IApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private void AttachSessionToken()
    {
        _http.DefaultRequestHeaders.Remove("X-Api-Token");
        var token = _accessor.HttpContext?.Session.GetString(SessionKeys.ApiToken);
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Add("X-Api-Token", token);
        }
    }

    public async Task<ApiResponse<T>?> GetAsync<T>(string path, CancellationToken ct = default)
    {
        AttachSessionToken();
        return await _http.GetFromJsonAsync<ApiResponse<T>>(path, JsonOptions, ct);
    }

    public async Task<ApiResponse<TResponse>?> PostAsync<TRequest, TResponse>(string path, TRequest? body, CancellationToken ct = default)
    {
        AttachSessionToken();
        var response = await _http.PostAsJsonAsync(path, body, JsonOptions, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions, ct);
    }

    public async Task<ApiResponse<TResponse>?> PutAsync<TRequest, TResponse>(string path, TRequest? body, CancellationToken ct = default)
    {
        AttachSessionToken();
        var response = await _http.PutAsJsonAsync(path, body, JsonOptions, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions, ct);
    }

    public async Task<ApiResponse<TResponse>?> PatchAsync<TResponse>(string path, CancellationToken ct = default)
    {
        AttachSessionToken();
        var response = await _http.PatchAsync(path, content: null, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions, ct);
    }

    public async Task<ApiResponse<TResponse>?> PatchAsync<TRequest, TResponse>(string path, TRequest? body, CancellationToken ct = default)
    {
        AttachSessionToken();
        var response = await _http.PatchAsJsonAsync(path, body, JsonOptions, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions, ct);
    }

    public async Task<ApiResponse<object>?> DeleteAsync(string path, CancellationToken ct = default)
    {
        AttachSessionToken();
        var response = await _http.DeleteAsync(path, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions, ct);
    }
}
