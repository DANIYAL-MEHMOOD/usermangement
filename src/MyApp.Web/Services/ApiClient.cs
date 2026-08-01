using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json;

namespace MyApp.Web.Services;

/// <summary>
/// Typed HTTP client the Web project uses for every call to MyApp.Api.
/// Before each request it checks the access token's expiry (stored in session
/// alongside the token itself) and silently exchanges the refresh token for a
/// new pair if it's expired or about to expire — pages never see a 401 from
/// an expired-but-refreshable session.
/// </summary>
public class ApiClient : IApiClient
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(30);

    // The API serializes with the default ASP.NET Core camelCase policy; matching
    // that here (rather than relying on the JsonSerializer default, which is
    // case-sensitive) is what makes every DTO below actually deserialize.
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

        if (string.IsNullOrEmpty(token)) return; // not logged in — let the call 401 and the page redirect

        var isExpiring = string.IsNullOrEmpty(expiryRaw)
            || !DateTime.TryParse(expiryRaw, null, DateTimeStyles.RoundtripKind, out var expiry)
            || expiry - RefreshSkew <= DateTime.UtcNow;

        if (!isExpiring)
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return;
        }

        await _refreshLock.WaitAsync(ct);
        try
        {
            // Another request may have already refreshed while we waited on the lock.
            token = session.GetString(SessionKeys.AccessToken);
            expiryRaw = session.GetString(SessionKeys.AccessTokenExpiry);
            if (!string.IsNullOrEmpty(expiryRaw)
                && DateTime.TryParse(expiryRaw, null, DateTimeStyles.RoundtripKind, out var freshExpiry)
                && freshExpiry - RefreshSkew > DateTime.UtcNow)
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return;
            }

            var refreshToken = session.GetString(SessionKeys.RefreshToken);
            if (string.IsNullOrEmpty(refreshToken))
            {
                session.Clear();
                return;
            }

            using var refreshResponse = await _http.PostAsJsonAsync("api/auth/refresh",
                new RefreshTokenRequest(refreshToken), JsonOptions, ct);
            var envelope = await refreshResponse.Content.ReadFromJsonAsync<ApiEnvelope<LoginResponseData>>(JsonOptions, ct);

            if (envelope is { Success: true, Data: not null })
            {
                session.SetString(SessionKeys.AccessToken, envelope.Data.AccessToken);
                session.SetString(SessionKeys.RefreshToken, envelope.Data.RefreshToken);
                session.SetString(SessionKeys.AccessTokenExpiry, envelope.Data.AccessTokenExpiry.ToString("o"));
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", envelope.Data.AccessToken);
            }
            else
            {
                // Refresh token is dead too — clear the session so the next page load bounces to /Login.
                session.Clear();
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<ApiEnvelope<T>?> GetAsync<T>(string path, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        return await _http.GetFromJsonAsync<ApiEnvelope<T>>(path, JsonOptions, ct);
    }

    public async Task<ApiEnvelope<TResponse>?> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        var response = await _http.PostAsJsonAsync(path, body, JsonOptions, ct);
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<TResponse>>(JsonOptions, ct);
    }

    public async Task<ApiEnvelope<TResponse>?> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        var response = await _http.PutAsJsonAsync(path, body, JsonOptions, ct);
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<TResponse>>(JsonOptions, ct);
    }

    public async Task<ApiEnvelope<TResponse>?> PatchAsync<TResponse>(string path, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        var response = await _http.PatchAsync(path, content: null, ct);
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<TResponse>>(JsonOptions, ct);
    }

    public async Task<ApiEnvelope<object>?> DeleteAsync(string path, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        var response = await _http.DeleteAsync(path, ct);
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(JsonOptions, ct);
    }
}
