namespace RaceDay_Web.Models;

// These mirror the JSON the RaceDay API returns. The website never touches the database directly.

public record AuthResponse(int UserId, int ProfileId, string Name, string Role, string Message);

public record MessageResponse(string Message);

public record ImageUploadResponse(string Url, string Message);

public record UserResponse(int UserId, string Name, string? Contact, string Email, string Gender, string Role, string? ProfilePictureUrl);

public record RouteResponse(int RouteId, string StartingPoint, string EndingPoint, decimal Distance);

public record EventResponse(
    int EventId, int OrganiserId, string Name, string? Description, string EventType,
    string Location, DateOnly Date, TimeOnly Time, RouteResponse? Route, string? BannerUrl);

public record EventSummaryResponse(
    int EventId, string Name, string EventType, string Location, DateOnly Date, TimeOnly Time,
    decimal? Distance, int CategoryCount, int EnrolmentCount, string? BannerUrl);

public record CategoryResponse(
    int CategoryId, int EventId, string CategoryName, decimal EntryFee, int? MaxParticipants, int RegisteredCount);

public record RegistrationResponse(
    int RegistrationId, int CategoryId, string CategoryName, int EventId, string EventName,
    DateOnly EventDate, string EventLocation, int ParticipantId, string ParticipantName,
    string RaceNum, DateOnly RegistrationDate, string Status);

public record ResultResponse(
    int ResultId, int RegistrationId, int EventId, string EventName, DateOnly EventDate, string CategoryName,
    int ParticipantId, string ParticipantName, string RaceNum, TimeSpan? FinishTime, int? Position,
    int TotalFinishers, bool IsPublished);

