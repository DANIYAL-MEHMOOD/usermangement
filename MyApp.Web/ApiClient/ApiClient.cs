using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MyApp.Web.Common;

namespace MyApp.Web.ApiClient;

/// <summary>
/// Typed HTTP client the Web project uses for every call to MyApp.Api.
/// Before each request it checks the access token's expiry (stored in session
/// alongside the token itself) and silently exchanges the refresh token for a
/// new pair if it's expired or about to expire.
/// </summary>
public class ApiClient : IApiClient
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private record RefreshTokenRequest(string RefreshToken);
    private record LoginResponseData(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry,
        int UserId, string Username, string FullName, string RoleName, bool MustChangePassword);

    private async Task EnsureValidTokenAsync(CancellationToken ct)
    {
        var session = _accessor.HttpContext?.Session;
        if (session is null) return;

        var token = session.GetString(SessionKeys.AccessToken);
        var expiryRaw = session.GetString(SessionKeys.AccessTokenExpiry);

        if (string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = null;
            return;
        }

        DateTime expiry = DateTime.MinValue;
        if (!string.IsNullOrEmpty(expiryRaw))
        {
            DateTime.TryParse(expiryRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out expiry);
        }

        if (expiry > DateTime.UtcNow.Add(RefreshSkew))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return;
        }

        var refreshToken = session.GetString(SessionKeys.RefreshToken);
        if (string.IsNullOrEmpty(refreshToken))
        {
            _http.DefaultRequestHeaders.Authorization = null;
            return;
        }

        await _refreshLock.WaitAsync(ct);
        try
        {
            token = session.GetString(SessionKeys.AccessToken);
            expiryRaw = session.GetString(SessionKeys.AccessTokenExpiry);
            if (!string.IsNullOrEmpty(expiryRaw) &&
                DateTime.TryParse(expiryRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out expiry) &&
                expiry > DateTime.UtcNow.Add(RefreshSkew))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return;
            }

            var result = await _http.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest(refreshToken), JsonOptions, ct);
            if (!result.IsSuccessStatusCode)
            {
                session.Remove(SessionKeys.AccessToken);
                session.Remove(SessionKeys.RefreshToken);
                session.Remove(SessionKeys.AccessTokenExpiry);
                _http.DefaultRequestHeaders.Authorization = null;
                return;
            }

            var refreshed = await result.Content.ReadFromJsonAsync<ApiResponse<LoginResponseData>>(JsonOptions, ct);
            if (refreshed is { Success: true, Data: not null })
            {
                session.SetString(SessionKeys.AccessToken, refreshed.Data.AccessToken);
                session.SetString(SessionKeys.RefreshToken, refreshed.Data.RefreshToken);
                session.SetString(SessionKeys.AccessTokenExpiry, refreshed.Data.AccessTokenExpiry.ToString("o"));
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.Data.AccessToken);
            }
            else
            {
                _http.DefaultRequestHeaders.Authorization = null;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<ApiResponse<T>?> GetAsync<T>(string path, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        return await _http.GetFromJsonAsync<ApiResponse<T>>(path, JsonOptions, ct);
    }

    public async Task<ApiResponse<TResponse>?> PostAsync<TRequest, TResponse>(string path, TRequest? body, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        var response = await _http.PostAsJsonAsync(path, body, JsonOptions, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions, ct);
    }

    public async Task<ApiResponse<TResponse>?> PutAsync<TRequest, TResponse>(string path, TRequest? body, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        var response = await _http.PutAsJsonAsync(path, body, JsonOptions, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions, ct);
    }

    public async Task<ApiResponse<TResponse>?> PatchAsync<TResponse>(string path, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        var response = await _http.PatchAsync(path, content: null, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions, ct);
    }

    public async Task<ApiResponse<TResponse>?> PatchAsync<TRequest, TResponse>(string path, TRequest? body, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        var response = await _http.PatchAsJsonAsync(path, body, JsonOptions, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions, ct);
    }

    public async Task<ApiResponse<object>?> DeleteAsync(string path, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        var response = await _http.DeleteAsync(path, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions, ct);
    }
}
