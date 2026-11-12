using LargeFileUpload.Application.Common.Interfaces;
using LargeFileUpload.Domain.Enums;
using LargeFileUpload.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LargeFileUpload.Infrastructure.Services;

public class ExpiredUploadCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredUploadCleanupBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

    public ExpiredUploadCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ExpiredUploadCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expired Upload Session Cleanup Service started.");

        // Wait 1 minute after application startup before first sweep
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepExpiredSessionsAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "An error occurred during expired upload session cleanup sweep.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task SweepExpiredSessionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UploadDbContext>();
        var blobStorageService = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();

        var now = DateTimeOffset.UtcNow;

        var expiredSessions = await dbContext.UploadSessions
            .Where(x => x.ExpiresAt < now && (x.Status == UploadStatus.Pending || x.Status == UploadStatus.Uploading))
            .Take(100)
            .ToListAsync(cancellationToken);

        if (expiredSessions.Count == 0)
        {
            _logger.LogDebug("Expired upload session sweep completed: No expired sessions found.");
            return;
        }

        _logger.LogInformation("Found {Count} expired upload sessions. Starting cleanup...", expiredSessions.Count);

        foreach (var session in expiredSessions)
        {
            try
            {
                // Purge uncommitted blocks / blob in Azure Storage
                await blobStorageService.DeleteBlobIfExistsAsync(session.BlobName, cancellationToken);
                session.Expire();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to purge blob for expired session {UploadId} (Blob: {BlobName})", session.Id, session.BlobName);
                session.Expire();
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully cleaned up {Count} expired upload sessions.", expiredSessions.Count);
    }
}
