using System.ComponentModel.DataAnnotations;

namespace LegalDocSystem.Application.DTOs.Auth;

public class VerifyEmailDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
