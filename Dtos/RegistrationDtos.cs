namespace RaceDay_api.Dtos;

public class RegistrationRequest
{
    public required int CategoryId { get; set; }
    /// <summary>Optional. If supplied it must be the logged-in participant's own ID.</summary>
    public int? ParticipantId { get; set; }
}
public record RegistrationResponse(
    int RegistrationId, int CategoryId, string CategoryName, int EventId, string EventName,
    int ParticipantId, string ParticipantName, string RaceNum, DateOnly RegistrationDate);
