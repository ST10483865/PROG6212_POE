using System.ComponentModel.DataAnnotations;

namespace RaceDay_api.Dtos;

public class ResultRequest
{
    public required int RegistrationId { get; set; }
    /// <example>01:45:30</example>
    public required TimeSpan FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int? Position { get; set; }
    public bool IsPublished { get; set; } = true;

}

public class ResultUpdateRequest
{
    /// <example>01:45:30</example>
    public required TimeSpan FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int? Position { get; set; }
    public bool IsPublished { get; set; } = true;

}

public record ResultResponse(
    int ResultId, int RegistrationId, int EventId, string EventName, DateOnly date, string CategoryName,
    int ParticipantId, string ParticipantName, string RaceNum, TimeSpan? FinishTime, int? Position,
    int TotalFinishers, bool IsPublished);
