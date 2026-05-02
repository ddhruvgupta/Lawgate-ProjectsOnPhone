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
    private readonly string _frontendBaseUrl;
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

        _frontendBaseUrl = configuration["App:FrontendBaseUrl"] ?? "http://localhost:5173";

        _client = new EmailClient(connectionString);
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(string toEmail, string firstName, string verificationLink)
    {
        var subject = "Verify your Lawgate account";
        var htmlBody = EmailTemplateLoader.Load("email-verification.html", new()
        {
            ["firstName"]        = EscapeHtml(firstName),
            ["verificationLink"] = verificationLink,
        });

        await SendAsync(toEmail, firstName, subject, htmlBody);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink)
    {
        var subject = "Reset your Lawgate password";
        var htmlBody = EmailTemplateLoader.Load("password-reset.html", new()
        {
            ["firstName"] = EscapeHtml(firstName),
            ["resetLink"] = resetLink,
        });

        await SendAsync(toEmail, firstName, subject, htmlBody);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string firstName)
    {
        var subject = "Welcome to Lawgate!";
        var htmlBody = EmailTemplateLoader.Load("welcome.html", new()
        {
            ["firstName"] = EscapeHtml(firstName),
            ["loginUrl"]  = _frontendBaseUrl,
        });

        await SendAsync(toEmail, firstName, subject, htmlBody);
    }

    public async Task SendTeamInviteEmailAsync(string toEmail, string firstName, string invitedByName, string companyName, string loginUrl, string temporaryPassword)
    {
        var subject = $"You've been added to {companyName} on Lawgate";
        var htmlBody = EmailTemplateLoader.Load("team-invite.html", new()
        {
            ["firstName"]         = EscapeHtml(firstName),
            ["companyName"]       = EscapeHtml(companyName),
            ["invitedByName"]     = EscapeHtml(invitedByName),
            ["email"]             = EscapeHtml(toEmail),
            ["temporaryPassword"] = EscapeHtml(temporaryPassword),
            ["loginUrl"]          = loginUrl,
        });

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
