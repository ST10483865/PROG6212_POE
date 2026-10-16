namespace RaceDay_api.Models
{
    public class Participant
    {
        public int ParticipantId { get; set; }
        public int UserId { get; set; }
        public string? MedicalInfo { get; set; }

        public User User { get; set; } = null!;
        public ICollection<Registration> Registration { get; set; } = new List<Registration>();
    }
}
