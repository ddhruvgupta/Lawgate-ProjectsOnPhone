using Azure;
using Azure.Communication.Email;
using LegalDocSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LegalDocSystem.Infrastructure.Services;

/// <summary>
/// Production email service using Azure Communication Services Email SDK.
/// Reads connection string from Email:AcsConnectionString and sender address
/// from Email:SenderAddress (both stored in Key Vault in production).
/// </summary>
public class AcsEmailService : IEmailService
{
    private readonly EmailClient _client;
    private readonly string _senderAddress;
    private readonly ILogger<AcsEmailService> _logger;

    public AcsEmailService(IConfiguration configuration, ILogger<AcsEmailService> logger)
    {
        var connectionString = configuration["Email:AcsConnectionString"]
            ?? throw new InvalidOperationException("Email:AcsConnectionString is not configured.");

        var senderDomain = configuration["Email:SenderAddress"]
            ?? throw new InvalidOperationException("Email:SenderAddress is not configured.");

        // SenderAddress config stores just the domain — build the full from address
        _senderAddress = senderDomain.Contains('@')
            ? senderDomain
            : $"DoNotReply@{senderDomain}";

        _client = new EmailClient(connectionString);
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(string toEmail, string firstName, string verificationLink)
    {
        var subject = "Verify your Lawgate account";
        var htmlBody = $"""
            <html><body style="font-family:sans-serif;color:#1a1a1a;max-width:600px;margin:auto;">
              <h2>Verify your email address</h2>
              <p>Hi {EscapeHtml(firstName)},</p>
              <p>Please verify your email address by clicking the button below.
                 This link expires in <strong>24 hours</strong>.</p>
              <p style="text-align:center;margin:32px 0;">
                <a href="{verificationLink}"
                   style="background:#1d4ed8;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:600;">
                  Verify Email
                </a>
              </p>
              <p style="color:#6b7280;font-size:13px;">
                Or copy this link into your browser:<br/>
                <a href="{verificationLink}">{verificationLink}</a>
              </p>
              <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0;"/>
              <p style="color:#9ca3af;font-size:12px;">
                If you did not create a Lawgate account, ignore this email.
              </p>
            </body></html>
            """;

        await SendAsync(toEmail, firstName, subject, htmlBody);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink)
    {
        var subject = "Reset your Lawgate password";
        var htmlBody = $"""
            <html><body style="font-family:sans-serif;color:#1a1a1a;max-width:600px;margin:auto;">
              <h2>Reset your password</h2>
              <p>Hi {EscapeHtml(firstName)},</p>
              <p>We received a request to reset your account password.
                 This link expires in <strong>1 hour</strong>.</p>
              <p style="text-align:center;margin:32px 0;">
                <a href="{resetLink}"
                   style="background:#dc2626;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:600;">
                  Reset Password
                </a>
              </p>
              <p style="color:#6b7280;font-size:13px;">
                Or copy this link into your browser:<br/>
                <a href="{resetLink}">{resetLink}</a>
              </p>
              <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0;"/>
              <p style="color:#9ca3af;font-size:12px;">
                If you did not request a password reset, ignore this email. Your password will not change.
              </p>
            </body></html>
            """;

        await SendAsync(toEmail, firstName, subject, htmlBody);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string firstName)
    {
        var subject = "Welcome to Lawgate!";
        var htmlBody = $"""
            <html><body style="font-family:sans-serif;color:#1a1a1a;max-width:600px;margin:auto;">
              <h2>Welcome to Lawgate</h2>
              <p>Hi {EscapeHtml(firstName)},</p>
              <p>Your account has been created. You can now log in and start managing your legal documents.</p>
              <p style="color:#9ca3af;font-size:12px;margin-top:32px;">The Lawgate Team</p>
            </body></html>
            """;

        await SendAsync(toEmail, firstName, subject, htmlBody);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var message = new EmailMessage(
            senderAddress: _senderAddress,
            recipients: new EmailRecipients([new EmailAddress(toEmail, toName)]),
            content: new EmailContent(subject) { Html = htmlBody }
        );

        try
        {
            var operation = await _client.SendAsync(WaitUntil.Completed, message);
            _logger.LogInformation(
                "ACS email sent to {Email}: {Subject} (operationId: {OperationId})",
                toEmail, subject, operation.Id);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "ACS email send failed to {Email}: {Subject}", toEmail, subject);
            throw;
        }
    }

    private static string EscapeHtml(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
