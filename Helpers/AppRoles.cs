namespace RaceDay_api.Helpers
{//start of namespace
    public static class AppRoles
    {//start of class
        public const string Organiser = "Organiser";
        public const string Participant = "Participant";

        //returns the conocial role name ,or null if the value is not a valid self registartion role
        public static string? Normalise(string?role) =>
            string.Equals(role?.Trim(), Organiser, StringComparison.OrdinalIgnoreCase) ? Organiser :
        string.Equals(role?.Trim(), Participant, StringComparison.OrdinalIgnoreCase) ? Participant :
        null;

    }//end of class
     

    public static class EventTypes
    {//start of Event Type class
        public static readonly string[] Allowed = { "Run", "Walk", "Cycle" };

        public static string? Normalise(string? type) =>
            Allowed.FirstOrDefault(t => string.Equals(t, type?.Trim(), StringComparison.OrdinalIgnoreCase));
    }//End of Event Type class
    public static class RegistrationStatuses
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";

        public static string? Normalise(string? status) =>
            string.Equals(status?.Trim(), Pending, StringComparison.OrdinalIgnoreCase) ? Pending :
            string.Equals(status?.Trim(), Confirmed, StringComparison.OrdinalIgnoreCase) ? Confirmed :
            null;
    }


}//end of namespace
