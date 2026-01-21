namespace LegalDocSystem.Application.DTOs.Auth;

public class UserDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
