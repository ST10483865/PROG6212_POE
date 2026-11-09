using System.ComponentModel.DataAnnotations;

namespace RaceDay_api.Dtos;

public class RegistrationRequest
{
    public required int CategoryId { get; set; }
    /// <summary>Optional. If supplied it must be the logged-in participant's own ID.</summary>
    public int? ParticipantId { get; set; }
}

public class RegistrationStatusRequest
{
    /// <summary>Pending or Confirmed.</summary>
    /// <example>Confirmed</example>
    [Required] public required string Status { get; set; }
}

public record RegistrationResponse(
    int RegistrationId, int CategoryId, string CategoryName, int EventId, string EventName,
    DateOnly EventDate, string EventLocation, int ParticipantId, string ParticipantName,
    string RaceNum, DateOnly RegistrationDate, string Status);
