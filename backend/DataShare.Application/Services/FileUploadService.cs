using System.Security.Cryptography;
using DataShare.Application.DTOs;
using DataShare.Application.Interfaces;
using DataShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataShare.Application.Services;

public class FileUploadService
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IFilePasswordHasher _passwordHasher;

    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".bat", ".cmd", ".com", ".scr", ".msi", ".sh",
        ".hta", ".vbs", ".vbe", ".js", ".jse", ".wsf", ".wsh",
        ".ps1", ".psc1", ".dll", ".iso", ".svg"
    };

    private const long MaxFileSizeBytes = 1_073_741_824;
    private const int MinExpirationDays = 1;
    private const int MaxExpirationDays = 7;
    private const int DefaultExpirationDays = 7;
    private const int MaxTagRetries = 2;

    public FileUploadService(
        IApplicationDbContext db,
        IFileStorageService storage,
        IFilePasswordHasher passwordHasher)
    {
        _db = db;
        _storage = storage;
        _passwordHasher = passwordHasher;
    }

    public async Task<(FileDto? file, UploadError? error, string? detail)> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        Guid? ownerId,
        int? expiresInDays,
        string? password,
        string[]? tags,
        string baseUrl,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(password))
            password = null;

        var extension = Path.GetExtension(fileName);
        if (BlockedExtensions.Contains(extension))
            return (null, UploadError.BlockedExtension, extension);

        if (fileSize > MaxFileSizeBytes)
            return (null, UploadError.FileTooLarge, null);

        var days = expiresInDays ?? DefaultExpirationDays;
        if (days < MinExpirationDays || days > MaxExpirationDays)
            return (null, UploadError.InvalidExpiration, null);

        if (password is { Length: < 6 })
            return (null, UploadError.PasswordTooShort, null);

        var downloadToken = GenerateDownloadToken();
        var storagePath = await _storage.SaveAsync(fileStream, fileName, ct);

        try
        {
            var storedFile = new StoredFile
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                OriginalName = fileName,
                MimeType = contentType,
                SizeBytes = fileSize,
                StoragePath = storagePath,
                DownloadToken = downloadToken,
                PasswordHash = password != null ? _passwordHasher.Hash(password) : null,
                ExpiresAt = DateTime.UtcNow.AddDays(days),
                CreatedAt = DateTime.UtcNow
            };

            _db.StoredFiles.Add(storedFile);

            var tagNames = await ResolveTagsWithRetryAsync(ownerId, tags, storedFile.Id, ct);
            await _db.SaveChangesAsync(ct);

            var downloadUrl = $"{baseUrl.TrimEnd('/')}/d/{downloadToken}";

            return (new FileDto(
                storedFile.Id,
                storedFile.OriginalName,
                storedFile.SizeBytes,
                storedFile.MimeType,
                storedFile.DownloadToken,
                downloadUrl,
                storedFile.ExpiresAt,
                storedFile.PasswordHash != null,
                tagNames,
                storedFile.CreatedAt), null, null);
        }
        catch
        {
            await _storage.DeleteAsync(storagePath, ct);
            throw;
        }
    }

    private async Task<string[]> ResolveTagsWithRetryAsync(
        Guid? ownerId, string[]? tags, Guid fileId, CancellationToken ct)
    {
        if (!ownerId.HasValue || tags is not { Length: > 0 })
            return [];

        var tagNames = tags
            .Where(t => !string.IsNullOrWhiteSpace(t) && t.Length <= 30)
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var tagName in tagNames)
        {
            var tag = await GetOrCreateTagAsync(ownerId.Value, tagName, ct);
            _db.FileTags.Add(new FileTag { FileId = fileId, TagId = tag.Id });
        }

        return tagNames;
    }

    private async Task<Tag> GetOrCreateTagAsync(Guid ownerId, string tagName, CancellationToken ct)
    {
        for (var attempt = 0; attempt <= MaxTagRetries; attempt++)
        {
            var existingTag = await _db.Tags
                .FirstOrDefaultAsync(t => t.OwnerId == ownerId && t.Name == tagName, ct);

            if (existingTag is not null)
                return existingTag;

            if (attempt < MaxTagRetries)
            {
                var newTag = new Tag
                {
                    Id = Guid.NewGuid(),
                    OwnerId = ownerId,
                    Name = tagName,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Tags.Add(newTag);

                try
                {
                    await _db.SaveChangesAsync(ct);
                    return newTag;
                }
                catch (DbUpdateException)
                {
                    // Race condition : un autre upload concurrent a créé le tag.
                    // Remove() sur une entité en état Added la fait passer à
                    // Detached, elle disparaît du ChangeTracker. Sans ça, le
                    // SaveChanges() suivant dans UploadAsync réessaierait
                    // d'insérer la même ligne et échouerait sur la contrainte
                    // unique (OwnerId, Name).
                    _db.Tags.Remove(newTag);
                }
            }
        }

        // Dernier recours : le tag existe forcément après les retries
        return (await _db.Tags.FirstAsync(t => t.OwnerId == ownerId && t.Name == tagName, ct));
    }

    private static string GenerateDownloadToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
