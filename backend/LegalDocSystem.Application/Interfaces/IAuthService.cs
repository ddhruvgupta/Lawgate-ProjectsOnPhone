using LegalDocSystem.Application.DTOs.Auth;

namespace LegalDocSystem.Application.Interfaces;

public interface IAuthService
{
    Task<TokenResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<TokenResponseDto> LoginAsync(LoginDto loginDto);
    Task<TokenResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> ValidateTokenAsync(string token);
}
