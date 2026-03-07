using LegalDocSystem.Domain.Enums;
using LegalDocSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LegalDocSystem.Infrastructure.BackgroundServices;

public class DocumentCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);
    private readonly TimeSpan _expirationThreshold = TimeSpan.FromHours(6);

    public DocumentCleanupService(
        IServiceProvider serviceProvider,
        ILogger<DocumentCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupPendingDocumentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up pending documents.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Document Cleanup Service is stopping.");
    }

    private async Task CleanupPendingDocumentsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var expirationDate = DateTime.UtcNow.Subtract(_expirationThreshold);

        var expiredDocuments = await context.Documents
            .Where(d => d.Status == DocumentStatus.Pending && d.CreatedAt < expirationDate)
            .ToListAsync(stoppingToken);

        if (expiredDocuments.Any())
        {
            _logger.LogInformation("Cleaning up {Count} expired pending documents.", expiredDocuments.Count);
            context.Documents.RemoveRange(expiredDocuments);
            await context.SaveChangesAsync(stoppingToken);
        }
    }
}
