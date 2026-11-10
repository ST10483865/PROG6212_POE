using System.Text.Json;

namespace RaceDay_Web.Services;

//Who is logged in. Stored in the MVC session after a successful API login.
public record CurrentUser(int UserId, int ProfileId, string Name, string Role)
{
    public bool IsOrganiser => Role == "Organiser";
    public bool IsParticipant => Role == "Participant";
    public string Initial => string.IsNullOrWhiteSpace(Name) ? "?" : char.ToUpperInvariant(Name.Trim()[0]).ToString();
}

public static class SessionExtensions
{
    private const string UserKey = "CurrentUser";

    public static void SetCurrentUser(this ISession session, CurrentUser user) =>
        session.SetString(UserKey, JsonSerializer.Serialize(user));

    public static CurrentUser? GetCurrentUser(this ISession session)
    {
        var json = session.GetString(UserKey);
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<CurrentUser>(json);
    }

}
