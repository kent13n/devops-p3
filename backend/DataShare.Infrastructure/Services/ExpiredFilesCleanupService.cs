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
    private static readonly TimeSpan HardDeleteRetention = TimeSpan.FromDays(30);

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
                await PurgeExpiredBlobsAsync(stoppingToken);
                await HardDeleteOldPurgedFilesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la purge des fichiers expirés");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    /// <summary>
    /// Étape 1 : supprime le blob des fichiers expirés mais non encore purgés,
    /// marque la ligne DB comme purgée pour la conserver dans l'historique.
    /// </summary>
    private async Task PurgeExpiredBlobsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        var totalPurged = 0;
        bool hasMore;

        do
        {
            var expiredFiles = await db.StoredFiles
                .Where(f => f.ExpiresAt < DateTime.UtcNow && !f.IsPurged)
                .Take(BatchSize)
                .ToListAsync(ct);

            hasMore = expiredFiles.Count == BatchSize;

            if (expiredFiles.Count == 0)
                break;

            foreach (var file in expiredFiles)
            {
                try
                {
                    await storage.DeleteAsync(file.StoragePath, ct);
                    file.IsPurged = true;
                    file.StoragePath = "";
                    totalPurged++;

                    _logger.LogInformation(
                        "Blob purgé : {FileId} ({OriginalName}), expiré depuis {Minutes} min",
                        file.Id, file.OriginalName, (int)(DateTime.UtcNow - file.ExpiresAt).TotalMinutes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Échec de la purge du blob {FileId} ({OriginalName})",
                        file.Id, file.OriginalName);
                }
            }

            await db.SaveChangesAsync(ct);
        } while (hasMore);

        if (totalPurged > 0)
            _logger.LogInformation("Purge des blobs terminée : {Total} fichier(s) traités", totalPurged);
    }

    /// <summary>
    /// Étape 2 : supprime définitivement les lignes DB des fichiers purgés depuis plus de 30 jours.
    /// </summary>
    private async Task HardDeleteOldPurgedFilesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cutoff = DateTime.UtcNow - HardDeleteRetention;
        var totalDeleted = 0;
        bool hasMore;

        do
        {
            var oldFiles = await db.StoredFiles
                .Where(f => f.IsPurged && f.ExpiresAt < cutoff)
                .Take(BatchSize)
                .ToListAsync(ct);

            hasMore = oldFiles.Count == BatchSize;

            if (oldFiles.Count == 0)
                break;

            db.StoredFiles.RemoveRange(oldFiles);
            totalDeleted += oldFiles.Count;

            await db.SaveChangesAsync(ct);
        } while (hasMore);

        if (totalDeleted > 0)
            _logger.LogInformation("Suppression définitive : {Total} fichier(s) retirés de l'historique (> 30j après expiration)", totalDeleted);
    }
}
