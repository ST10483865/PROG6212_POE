namespace RaceDay_api.Models;

public class User
{//start of class
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public Organiser? Organiser { get; set; }
    public Participant? Participant { get; set; }


}//end of class

