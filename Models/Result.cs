namespace RaceDay_api.Models;

public class Result
{//start of class
    public int ResultId { get; set; }
    public int RegistrationId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public Registration Registration { get; set; } = null!;


}//end of class


