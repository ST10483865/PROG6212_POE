using System.ComponentModel.DataAnnotations;

namespace RaceDay_api.Dtos;

public class EventRequest
{//satart of events class
    /// <example>Soweto Marathon</example>
    [Required, StringLength(200)] public required string Name { get; set; }
    [StringLength(1000)] public string? Description { get; set; }
    /// <summary>Run, Walk or Cycle.</summary>
    /// <example>Run</example>
    [Required] public required string EventType { get; set; }
    /// <example>Johannesburg CBD</example>
    [Required, StringLength(200)] public required string Location { get; set; }
    /// <example>2026-11-15</example>
    public required DateOnly Date { get; set; }
    /// <example>06:30:00</example>
    public required TimeOnly Time { get; set; }
    [Required, StringLength(200)] public required string StartingPoint { get; set; }
    [Required, StringLength(200)] public required string EndingPoint { get; set; }
    /// <summary>Route distance in kilometres.</summary>
    /// <example>21.1</example>
    [Range(typeof(decimal), "0.1", "1000")] public required decimal Distance { get; set; }
}//end of class

public class EventUpdateRequest
{
    [Required, StringLength(200)] public required string Name { get; set; }
    [StringLength(1000)] public string? Description { get; set; }
    [Required] public required string EventType { get; set; }
    [Required, StringLength(200)] public required string Location { get; set; }
    public required DateOnly Date { get; set; }
    public required TimeOnly Time { get; set; }
    /// <summary>Optional. Only changes the route if supplied.</summary>
    [StringLength(200)] public string? StartingPoint { get; set; }
    [StringLength(200)] public string? EndingPoint { get; set; }
    [Range(typeof(decimal), "0.1", "1000")] public decimal? Distance { get; set; }
}

public record RouteResponse(int RouteId, string StartingPoint, string EndingPoint, decimal Distance);

public record EventResponse(
    int EventId, int OrganiserId, string Name, string? Description, string EventType,
    string Location, DateOnly Date, TimeOnly Time, RouteResponse? Route);


