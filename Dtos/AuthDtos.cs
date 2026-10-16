using System.ComponentModel.DataAnnotations;

namespace RaceDay_api.Dtos;

public class RegisterRequest
{
    /// <example>Thabo Nkosi</example>
    [Required, StringLength(100)] public required string Name { get; set; }
    /// <example>thabo@example.com</example>
    [Required, EmailAddress, StringLength(200)] public required string Email { get; set; }
    /// <summary>Minimum 8 characters. Stored only as a salted hash.</summary>
    [Required, StringLength(100, MinimumLength = 8)] public required string Password { get; set; }
    /// <example>Male</example>
    [Required, StringLength(20)] public required string Gender { get; set; }
    /// <example>0821234567</example>
    [StringLength(20)] public string? Contact { get; set; }
    /// <summary>Either "Organiser" or "Participant". No other role can be self-assigned.</summary>
    /// <example>Participant</example>
    [Required] public required string Role { get; set; }
    /// <summary>Organisers only. Defaults to "Independent" if omitted.</summary>
    [StringLength(200)] public string? ClubName { get; set; }
    /// <summary>Participants only. Optional medical notes.</summary>
    [StringLength(255)] public string? MedicalInfo { get; set; }
}


public class LoginRequest
{
    [Required, EmailAddress] public required string Email { get; set; }
    [Required] public required string Password { get; set; }
}

public record AuthResponse(int UserId, int ProfileId, string Name, string Role, string Message);

public record MessageResponse(string Message);