using DataShare.Application.Interfaces;
using DataShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataShare.Infrastructure.Services;

public class ExpiredFilesCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredFilesCleanupService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private const int BatchSize = 500;

    public ExpiredFilesCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredFilesCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeExpiredFilesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la purge des fichiers expirés");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task PurgeExpiredFilesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        var totalPurged = 0;
        bool hasMore;

        do
        {
            var expiredFiles = await db.StoredFiles
                .Where(f => f.ExpiresAt < DateTime.UtcNow)
                .Take(BatchSize)
                .ToListAsync(ct);

            hasMore = expiredFiles.Count == BatchSize;

            if (expiredFiles.Count == 0)
                break;

            _logger.LogInformation("Purge batch : {Count} fichier(s) expiré(s) détecté(s)", expiredFiles.Count);

            foreach (var file in expiredFiles)
            {
                try
                {
                    await storage.DeleteAsync(file.StoragePath, ct);
                    db.StoredFiles.Remove(file);
                    totalPurged++;

                    _logger.LogInformation(
                        "Fichier purgé : {FileId} ({OriginalName}), expiré depuis {Minutes} min",
                        file.Id, file.OriginalName, (int)(DateTime.UtcNow - file.ExpiresAt).TotalMinutes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Échec de la purge du fichier {FileId} ({OriginalName})",
                        file.Id, file.OriginalName);
                }
            }

            await db.SaveChangesAsync(ct);
        } while (hasMore);

        if (totalPurged > 0)
            _logger.LogInformation("Purge terminée : {Total} fichier(s) supprimé(s)", totalPurged);
    }
}
