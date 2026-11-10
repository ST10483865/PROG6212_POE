namespace RaceDay_Web.Services;

//The outcome of one call to the RaceDay API
public class ApiResult<T>
{
    public bool Success { get; private init; }
    public int StatusCode { get; private init; }
    public T? Data { get; private init; }
    //A friendly message that is safe to show to the user
    public string? Error { get; private init; }
    //True when the API says the login session has ended
    public bool SessionExpired { get; private init; }

    public static ApiResult<T> Ok(T data, int statusCode = 200) =>
        new() { Success = true, StatusCode = statusCode, Data = data };

    public static ApiResult<T> Fail(int statusCode, string error, bool sessionExpired = false) =>
        new() { Success = false, StatusCode = statusCode, Error = error, SessionExpired = sessionExpired };
}

//A file (for example an image) fetched from the API
public record ApiFile(byte[] Content, string ContentType);

