using System.ComponentModel.DataAnnotations;

namespace RaceDay_Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like a valid email address.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select your gender.")]
    public string Gender { get; set; } = string.Empty;

    [RegularExpression(@"^[0-9+ ]{7,20}$", ErrorMessage = "Enter a valid contact number (digits only, 7 to 20 characters).")]
    [Display(Name = "Contact number")]
    public string? Contact { get; set; }

    [Required]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Please choose Organiser or Participant.")]
    public string Role { get; set; } = "Participant";

    [StringLength(200)]
    [Display(Name = "Club or organisation")]
    public string? ClubName { get; set; }

    [StringLength(255)]
    [Display(Name = "Medical notes (optional)")]
    public string? MedicalInfo { get; set; }
}
