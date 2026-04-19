using DataShare.Application.Interfaces;
using DataShare.Domain.Entities;
using DataShare.Infrastructure.Data;
using DataShare.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DataShare.Tests.Unit.Services;

/// <summary>
/// Tests sur les méthodes internes de l'ExpiredFilesCleanupService.
/// Utilise un vrai DbContext InMemory pour éviter le mock complexe du ChangeTracker.
/// </summary>
public class ExpiredFilesCleanupServiceTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly ApplicationDbContext _db;
    private readonly FakeFileStorageService _storage;

    public ExpiredFilesCleanupServiceTests()
    {
        // Nom fixe pour que tous les scopes partagent la même base InMemory
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName));
        // Singleton pour partager l'instance entre le scope du test et celui du service
        _storage = new FakeFileStorageService();
        services.AddSingleton<IFileStorageService>(_storage);
        _provider = services.BuildServiceProvider();
        _db = _provider.GetRequiredService<ApplicationDbContext>();
    }

    public void Dispose()
    {
        _provider.Dispose();
        GC.SuppressFinalize(this);
    }

    private StoredFile CreateFile(DateTime expiresAt, bool isPurged = false, string? path = "path.bin") => new()
    {
        Id = Guid.NewGuid(),
        OriginalName = "f.txt",
        MimeType = "text/plain",
        SizeBytes = 10,
        StoragePath = path ?? "",
        DownloadToken = Guid.NewGuid().ToString("N"),
        ExpiresAt = expiresAt,
        IsPurged = isPurged,
        CreatedAt = DateTime.UtcNow.AddDays(-10)
    };

    private ExpiredFilesCleanupService BuildService()
    {
        var scopeFactory = _provider.GetRequiredService<IServiceScopeFactory>();
        return new ExpiredFilesCleanupService(scopeFactory, NullLogger<ExpiredFilesCleanupService>.Instance);
    }

    [Fact]
    public async Task PurgeExpiredBlobsAsync_ExpiredFileNotPurged_DeletesBlobAndMarksPurged()
    {
        var file = CreateFile(DateTime.UtcNow.AddMinutes(-5));
        _db.StoredFiles.Add(file);
        await _db.SaveChangesAsync();

        var service = BuildService();
        await service.PurgeExpiredBlobsAsync(CancellationToken.None);

        _storage.DeletedPaths.Should().Contain("path.bin");
        _db.ChangeTracker.Clear();
        var refreshed = await _db.StoredFiles.SingleAsync(f => f.Id == file.Id);
        refreshed.IsPurged.Should().BeTrue();
        refreshed.StoragePath.Should().BeEmpty();
    }

    [Fact]
    public async Task PurgeExpiredBlobsAsync_AlreadyPurgedFile_IsSkipped()
    {
        var file = CreateFile(DateTime.UtcNow.AddDays(-10), isPurged: true, path: "");
        _db.StoredFiles.Add(file);
        await _db.SaveChangesAsync();

        var service = BuildService();
        await service.PurgeExpiredBlobsAsync(CancellationToken.None);

        _storage.DeletedPaths.Should().BeEmpty();
    }

    [Fact]
    public async Task PurgeExpiredBlobsAsync_ActiveFile_IsUntouched()
    {
        var file = CreateFile(DateTime.UtcNow.AddDays(1));
        _db.StoredFiles.Add(file);
        await _db.SaveChangesAsync();

        var service = BuildService();
        await service.PurgeExpiredBlobsAsync(CancellationToken.None);

        _storage.DeletedPaths.Should().BeEmpty();
        _db.ChangeTracker.Clear();
        var refreshed = await _db.StoredFiles.SingleAsync(f => f.Id == file.Id);
        refreshed.IsPurged.Should().BeFalse();
    }

    [Fact]
    public async Task HardDeleteOldPurgedFilesAsync_PurgedOver30Days_RemovesRow()
    {
        var oldPurged = CreateFile(DateTime.UtcNow.AddDays(-35), isPurged: true);
        _db.StoredFiles.Add(oldPurged);
        await _db.SaveChangesAsync();

        var service = BuildService();
        await service.HardDeleteOldPurgedFilesAsync(CancellationToken.None);

        _db.ChangeTracker.Clear();
        var remaining = await _db.StoredFiles.CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task HardDeleteOldPurgedFilesAsync_PurgedUnder30Days_KeepsRow()
    {
        var recentlyPurged = CreateFile(DateTime.UtcNow.AddDays(-5), isPurged: true);
        _db.StoredFiles.Add(recentlyPurged);
        await _db.SaveChangesAsync();

        var service = BuildService();
        await service.HardDeleteOldPurgedFilesAsync(CancellationToken.None);

        _db.ChangeTracker.Clear();
        var remaining = await _db.StoredFiles.CountAsync();
        remaining.Should().Be(1);
    }

    private class FakeFileStorageService : IFileStorageService
    {
        public List<string> DeletedPaths { get; } = new();

        public Task DeleteAsync(string storagePath, CancellationToken ct = default)
        {
            DeletedPaths.Add(storagePath);
            return Task.CompletedTask;
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default)
            => Task.FromResult<Stream>(new MemoryStream());

        public Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default)
            => Task.FromResult(fileName);
    }
}
