using System.ComponentModel.DataAnnotations;

namespace RaceDay_api.Dtos;

public class ResultRequest
{
    public required int RegistrationId { get; set; }
    /// <example>01:45:30</example>
    public required TimeSpan FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int? Position { get; set; }
}

public class ResultUpdateRequest
{
    /// <example>01:45:30</example>
    public required TimeSpan FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int? Position { get; set; }
}

public record ResultResponse(
    int ResultId, int RegistrationId, int EventId, string EventName, string CategoryName,
    int ParticipantId, string ParticipantName, string RaceNum, TimeSpan? FinishTime, int? Position);
