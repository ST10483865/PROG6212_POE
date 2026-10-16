namespace RaceDay_api.Models
{
    public class Organiser
    {
        public int OrganiserId { get; set; }
        public int UserId { get; set; }
        public string OrganiserName { get; set; } = string.Empty;
        public string ClubName { get; set; } = string.Empty;

        public User User { get; set; } = null!;
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
