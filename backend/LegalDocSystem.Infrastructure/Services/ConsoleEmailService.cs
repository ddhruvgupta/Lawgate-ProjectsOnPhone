using LegalDocSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LegalDocSystem.Infrastructure.Services;

/// <summary>
/// Development email service that logs email content to the console and writes
/// to the logs/emails/ folder. Swap this for an SMTP / SendGrid implementation
/// in production by registering a different IEmailService implementation.
/// </summary>
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailVerificationAsync(string toEmail, string firstName, string verificationLink)
    {
        var body = $"""
            ============================================================
            EMAIL VERIFICATION — DEV ONLY
            To: {toEmail}
            Subject: Verify your Lawgate account
            ============================================================
            Hi {firstName},

            Please verify your email address by clicking the link below:
            {verificationLink}

            This link expires in 24 hours.

            If you did not create a Lawgate account, you can safely ignore this email.
            ============================================================
            """;

        _logger.LogInformation("[DEV EMAIL] Email verification for {Email}:\n{Body}", toEmail, body);
        WriteEmailToFile($"verify-{SanitiseEmail(toEmail)}-{Timestamp()}.txt", body);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink)
    {
        var body = $"""
            ============================================================
            PASSWORD RESET — DEV ONLY
            To: {toEmail}
            Subject: Reset your Lawgate password
            ============================================================
            Hi {firstName},

            We received a request to reset the password for your account.
            Click the link below to set a new password:
            {resetLink}

            This link expires in 1 hour.

            If you did not request a password reset, you can safely ignore this email.
            ============================================================
            """;

        _logger.LogInformation("[DEV EMAIL] Password reset for {Email}:\n{Body}", toEmail, body);
        WriteEmailToFile($"reset-{SanitiseEmail(toEmail)}-{Timestamp()}.txt", body);
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string toEmail, string firstName)
    {
        var body = $"""
            ============================================================
            WELCOME EMAIL — DEV ONLY
            To: {toEmail}
            Subject: Welcome to Lawgate!
            ============================================================
            Hi {firstName},

            Welcome to Lawgate — your legal document management platform.

            You can log in at any time and start managing your projects.
            ============================================================
            """;

        _logger.LogInformation("[DEV EMAIL] Welcome email for {Email}:\n{Body}", toEmail, body);
        WriteEmailToFile($"welcome-{SanitiseEmail(toEmail)}-{Timestamp()}.txt", body);
        return Task.CompletedTask;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void WriteEmailToFile(string fileName, string content)
    {
        try
        {
            var dir = Path.Combine("logs", "emails");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, fileName), content);
        }
        catch
        {
            // Non-critical — swallow so email logging never breaks the request
        }
    }

    private static string SanitiseEmail(string email) =>
        email.Replace('@', '_').Replace('.', '_');

    private static string Timestamp() =>
        DateTime.UtcNow.ToString("yyyyMMddHHmmss");
}
