namespace RaceDay_api.Models;
//start of namespace
public class Category
{//start of class
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }
    public int? MaxParticipants { get; set; }

    public Event Event { get; set; } = null!;
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();

}//end of class

