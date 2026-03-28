using LegalDocSystem.Application.DTOs.Auth;
using LegalDocSystem.Application.DTOs.Common;
using LegalDocSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LegalDocSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new company and admin user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<TokenResponseDto>.ErrorResponse(
                    "Invalid input", 
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            var result = await _authService.RegisterAsync(registerDto);
            
            return Ok(ApiResponse<TokenResponseDto>.SuccessResponse(
                result, 
                "Registration successful"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<TokenResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, ApiResponse<TokenResponseDto>.ErrorResponse(
                "An error occurred during registration"));
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<TokenResponseDto>.ErrorResponse(
                    "Invalid input",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            var result = await _authService.LoginAsync(loginDto);
            
            return Ok(ApiResponse<TokenResponseDto>.SuccessResponse(
                result,
                "Login successful"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login failed: {Message}", ex.Message);
            return Unauthorized(ApiResponse<TokenResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, ApiResponse<TokenResponseDto>.ErrorResponse(
                "An error occurred during login"));
        }
    }

    /// <summary>
    /// Refresh access token using a valid refresh token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(ApiResponse<TokenResponseDto>.ErrorResponse("Refresh token is required"));

            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(ApiResponse<TokenResponseDto>.SuccessResponse(result, "Token refreshed successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Token refresh failed");
            return Unauthorized(ApiResponse<TokenResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, ApiResponse<TokenResponseDto>.ErrorResponse("An error occurred during token refresh"));
        }
    }

    /// <summary>
    /// Validate JWT token
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        try
        {
            var isValid = await _authService.ValidateTokenAsync(request.Token);
            int? userId = null;
            
            if (isValid)
            {
                userId = _jwtTokenService.GetUserIdFromToken(request.Token);
            }
            
            return Ok(ApiResponse<object>.SuccessResponse(
                new { isValid, userId },
                isValid ? "Token is valid" : "Token is invalid"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "An error occurred during token validation"));
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<ApiResponse<object>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var companyId = User.FindFirst("CompanyId")?.Value;

            var userInfo = new
            {
                userId = userId,
                email = email,
                role = role,
                companyId = companyId
            };

            return Ok(ApiResponse<object>.SuccessResponse(userInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "An error occurred while retrieving user information"));
        }
    }

    /// <summary>
    /// Request a password-reset email
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object?>>> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object?>.ErrorResponse("Invalid input",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        // Always return 200 to prevent email enumeration
        await _authService.ForgotPasswordAsync(dto.Email);
        return Ok(ApiResponse<object?>.SuccessResponse(null,
            "If that email is registered you will receive a password reset link shortly."));
    }

    /// <summary>
    /// Reset password using a token from the reset email
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object?>>> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object?>.ErrorResponse("Invalid input",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var success = await _authService.ResetPasswordAsync(dto.Token, dto.NewPassword);
        if (!success)
            return BadRequest(ApiResponse<object?>.ErrorResponse("Invalid or expired reset token."));

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Password reset successful. You can now log in."));
    }

    /// <summary>
    /// Verify email with a token from the verification email
    /// </summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object?>>> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object?>.ErrorResponse("Invalid input",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var success = await _authService.VerifyEmailAsync(dto.Token);
        if (!success)
            return BadRequest(ApiResponse<object?>.ErrorResponse("Invalid or expired verification token."));

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Email verified successfully."));
    }

    /// <summary>
    /// Resend the email verification link
    /// </summary>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object?>>> ResendVerification([FromBody] ResendVerificationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object?>.ErrorResponse("Invalid input",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        await _authService.ResendVerificationEmailAsync(dto.Email);
        return Ok(ApiResponse<object?>.SuccessResponse(null,
            "If that email is registered and unverified, a new verification link has been sent."));
    }
}
