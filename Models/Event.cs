namespace RaceDay_api.Models;

public class Event
{//start of Class
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    //run,walk or Cycle
    public string EventType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }

    public Organiser Organiser { get; set; } = null!;
    public ICollection<RaceRoute> Routes { get; set; } = new List<RaceRoute>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}//end of class

