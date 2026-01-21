namespace LegalDocSystem.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(int userId, string email, string role, int companyId);
    string GenerateRefreshToken();
    bool ValidateToken(string token);
    int? GetUserIdFromToken(string token);
}
