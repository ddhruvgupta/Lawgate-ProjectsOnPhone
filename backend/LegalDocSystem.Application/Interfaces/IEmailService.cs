namespace LegalDocSystem.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string firstName, string verificationLink);
    Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink);
    Task SendWelcomeEmailAsync(string toEmail, string firstName);
    Task SendTeamInviteEmailAsync(string toEmail, string firstName, string invitedByName, string companyName, string loginUrl, string temporaryPassword);
}
