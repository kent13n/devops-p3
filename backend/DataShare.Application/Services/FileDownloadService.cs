using DataShare.Application.DTOs;
using DataShare.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataShare.Application.Services;

public class FileDownloadService
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IFilePasswordHasher _passwordHasher;

    public FileDownloadService(
        IApplicationDbContext db,
        IFileStorageService storage,
        IFilePasswordHasher passwordHasher)
    {
        _db = db;
        _storage = storage;
        _passwordHasher = passwordHasher;
    }

    public async Task<(FileMetadataDto? metadata, DownloadError? error)> GetMetadataAsync(
        string token, CancellationToken ct = default)
    {
        var file = await _db.StoredFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.DownloadToken == token, ct);

        if (file is null)
            return (null, DownloadError.NotFound);

        if (file.ExpiresAt < DateTime.UtcNow)
            return (null, DownloadError.Expired);

        var metadata = new FileMetadataDto(
            file.OriginalName,
            file.SizeBytes,
            file.MimeType,
            file.ExpiresAt,
            file.PasswordHash != null);

        return (metadata, null);
    }

    public async Task<DownloadResult> DownloadAsync(
        string token, string? password, CancellationToken ct = default)
    {
        var file = await _db.StoredFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.DownloadToken == token, ct);

        if (file is null)
            return new DownloadResult.Failure(DownloadError.NotFound);

        if (file.ExpiresAt < DateTime.UtcNow)
            return new DownloadResult.Failure(DownloadError.Expired);

        if (file.PasswordHash != null)
        {
            if (string.IsNullOrEmpty(password))
                return new DownloadResult.Failure(DownloadError.PasswordRequired);

            if (!_passwordHasher.Verify(file.PasswordHash, password))
                return new DownloadResult.Failure(DownloadError.InvalidPassword);
        }

        var stream = await _storage.OpenReadAsync(file.StoragePath, ct);
        return new DownloadResult.Success(stream, file.OriginalName, file.MimeType, file.SizeBytes);
    }
}
