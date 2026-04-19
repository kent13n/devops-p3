using DataShare.Application.DTOs;
using DataShare.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataShare.Application.Services;

public class FileListService
{
    private readonly IApplicationDbContext _db;

    public FileListService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<FileHistoryItem[]> GetUserFilesAsync(
        Guid userId,
        FileStatusFilter status,
        string baseUrl,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var query = _db.StoredFiles
            .AsNoTracking()
            .Where(f => f.OwnerId == userId);

        query = status switch
        {
            FileStatusFilter.Active => query.Where(f => f.ExpiresAt > now),
            FileStatusFilter.Expired => query.Where(f => f.ExpiresAt <= now),
            _ => query
        };

        var files = await query
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                f.Id,
                f.OriginalName,
                f.SizeBytes,
                f.MimeType,
                f.DownloadToken,
                f.ExpiresAt,
                f.IsPurged,
                f.PasswordHash,
                f.CreatedAt,
                Tags = f.FileTags.Select(ft => ft.Tag.Name).ToArray()
            })
            .ToArrayAsync(ct);

        var prefix = baseUrl.TrimEnd('/');

        return files.Select(f => new FileHistoryItem(
            f.Id,
            f.OriginalName,
            f.SizeBytes,
            f.MimeType,
            (f.ExpiresAt <= now || f.IsPurged) ? null : $"{prefix}/d/{f.DownloadToken}",
            f.ExpiresAt,
            f.ExpiresAt <= now,
            f.IsPurged,
            f.PasswordHash != null,
            f.Tags,
            f.CreatedAt
        )).ToArray();
    }
}
