using System.ComponentModel.DataAnnotations;

namespace LegalDocSystem.Application.DTOs.Auth;

public class RegisterDto
{
    [Required]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string CompanyEmail { get; set; } = string.Empty;

    [Phone]
    public string? CompanyPhone { get; set; }

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }
}
