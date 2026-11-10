using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;

namespace RaceDay_Web.Services;

public class ApiClient
{
    private const string ApiCookieName = "RaceDay.Session"; // must match the cookie name set in the API's Program.cs
    private const string CookieSessionKey = "ApiCookie";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    // ---------- public methods ----------

    public Task<ApiResult<T>> GetAsync<T>(string url) =>
        SendAsync<T>(HttpMethod.Get, url, null);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
        SendAsync<T>(HttpMethod.Post, url, Serialise(body));

    public Task<ApiResult<T>> PutAsync<T>(string url, object body) =>
        SendAsync<T>(HttpMethod.Put, url, Serialise(body));

    public Task<ApiResult<T>> DeleteAsync<T>(string url) =>
        SendAsync<T>(HttpMethod.Delete, url, null);

    /// <summary>Uploads an image to the API (form field name: file).</summary>
    public async Task<ApiResult<T>> PostFileAsync<T>(string url, IFormFile file)
    {
        var form = new MultipartFormDataContent();
        var stream = file.OpenReadStream();
        var part = new StreamContent(stream);
        part.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        form.Add(part, "file", file.FileName);
        return await SendAsync<T>(HttpMethod.Post, url, form);
    }

    /// <summary>Downloads a file (such as an image) from the API.</summary>
    public async Task<ApiResult<ApiFile>> GetFileAsync(string url)
    {
        try
        {
            using var request = Build(HttpMethod.Get, url, null);
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return ApiResult<ApiFile>.Fail((int)response.StatusCode, FriendlyMessage(response.StatusCode));

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var type = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            return ApiResult<ApiFile>.Ok(new ApiFile(bytes, type), (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ApiResult<ApiFile>.Fail(503, ServiceDownMessage);
        }
    }

    // ---------- internals ----------

    private const string ServiceDownMessage =
        "The RaceDay service is not available right now. Please try again in a moment.";

    private static StringContent? Serialise(object? body) =>
        body is null ? null : new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");

    private HttpRequestMessage Build(HttpMethod method, string url, HttpContent? content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        var cookie = Session.GetString(CookieSessionKey);
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        return request;
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content)
    {
        var hadCookie = !string.IsNullOrEmpty(Session.GetString(CookieSessionKey));
        try
        {
            using var request = Build(method, url, content);
            using var response = await _http.SendAsync(request);
            CaptureCookie(response);

            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                if (string.IsNullOrWhiteSpace(body)) return ApiResult<T>.Ok(default!, (int)response.StatusCode);
                var data = JsonSerializer.Deserialize<T>(body, Json);
                return ApiResult<T>.Ok(data!, (int)response.StatusCode);
            }

            // 401 on a call that carried a session cookie = the API session ended (e.g. timed out)
            if (response.StatusCode == HttpStatusCode.Unauthorized && hadCookie)
            {
                Session.Clear();
                return ApiResult<T>.Fail(401, "Your session has expired. Please log in again.", sessionExpired: true);
            }

            return ApiResult<T>.Fail((int)response.StatusCode, ExtractMessage(response.StatusCode, body));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ApiResult<T>.Fail(503, ServiceDownMessage);
        }
        catch (JsonException)
        {
            return ApiResult<T>.Fail(500, "We received an unexpected reply from the RaceDay service.");
        }
    }

    /// <summary>Keeps the API's session cookie (set at login, cleared at logout) in this user's MVC session.</summary>
    private void CaptureCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return;

        foreach (var header in values)
        {
            if (!header.StartsWith(ApiCookieName + "=", StringComparison.OrdinalIgnoreCase)) continue;

            var pair = header.Split(';')[0];
            var value = pair[(ApiCookieName.Length + 1)..];
            if (string.IsNullOrEmpty(value)) Session.Remove(CookieSessionKey);
            else Session.SetString(CookieSessionKey, pair);
        }
    }

    /// <summary>Uses the API's own message when it sent one; otherwise a friendly standard message.</summary>
    private static string ExtractMessage(HttpStatusCode status, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(message.GetString()))
                return message.GetString()!;
        }
        catch (JsonException) { /* not JSON - fall through */ }

        return FriendlyMessage(status);
    }

    private static string FriendlyMessage(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => "Some of the information you entered is not valid. Please check it and try again.",
        HttpStatusCode.Unauthorized => "Please log in to continue.",
        HttpStatusCode.Forbidden => "You do not have permission to do that.",
        HttpStatusCode.NotFound => "That item could not be found.",
        HttpStatusCode.Conflict => "That action conflicts with existing information.",
        _ => "Something went wrong. Please try again."
    };
}

