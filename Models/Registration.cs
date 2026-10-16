namespace RaceDay_api.Models;

public class Registration
{//start of class
    public int RegistrationId { get; set; }
    public int CategoryId { get; set; }
    public int ParticipantId { get; set; }
    public string RaceNum { get; set; } = string.Empty;
    public DateOnly RegistrationDate { get; set; }

    public Category Category { get; set; } = null!;
    public Participant Participant { get; set; } = null!;
    public Result? Result { get; set; }

}//end of class

