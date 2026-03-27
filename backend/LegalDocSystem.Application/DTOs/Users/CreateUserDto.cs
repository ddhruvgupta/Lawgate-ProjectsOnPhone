using System.ComponentModel.DataAnnotations;
using LegalDocSystem.Domain.Enums;

namespace LegalDocSystem.Application.DTOs.Users;

public class CreateUserDto
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    public UserRole Role { get; set; } = UserRole.User;
}
