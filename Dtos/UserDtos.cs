using System.ComponentModel.DataAnnotations;

namespace RaceDay_api.Dtos;

public class UpdatedUserRequest
{//start of class
    [Required, StringLength(100)] public required string Name { get; set; }
    [StringLength(20)] public string? Contact { get; set; }
    [Required, EmailAddress, StringLength(200)] public required string Email { get; set; }
    [Required, StringLength(20)] public required string Gender { get; set; }
}//end of class
public record UserResponse(int UserId, string Name, string? Contact, string Email, string Gender, string Role);

