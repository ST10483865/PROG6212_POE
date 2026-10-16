namespace RaceDay_api.Models;

public class RaceRoute
{
    public int RouteId { get; set; }
    public int EventId { get; set; }
    public string StartingPoint { get; set; } = string.Empty;
    public string EndingPoint { get; set; } = string.Empty;
    /// <summary>Distance in kilometres.</summary>
    public decimal Distance { get; set; }

    public Event Event { get; set; } = null!;
}
