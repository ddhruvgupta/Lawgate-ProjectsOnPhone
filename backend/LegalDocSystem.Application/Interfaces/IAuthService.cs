using LegalDocSystem.Application.DTOs.Auth;

namespace LegalDocSystem.Application.Interfaces;

public interface IAuthService
{
    Task<TokenResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<TokenResponseDto> LoginAsync(LoginDto loginDto);
    Task<TokenResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> ValidateTokenAsync(string token);
    Task ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task<bool> VerifyEmailAsync(string token);
    Task ResendVerificationEmailAsync(string email);
}
