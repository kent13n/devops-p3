using DataShare.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataShare.Application.Services;

public class FileDeleteService
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorageService _storage;

    public FileDeleteService(IApplicationDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid fileId, CancellationToken ct = default)
    {
        // Retourne false pour NOT_FOUND et FORBIDDEN de manière indiscernable (anti-IDOR)
        var file = await _db.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && f.OwnerId == userId, ct);

        if (file is null)
            return false;

        await _storage.DeleteAsync(file.StoragePath, ct);
        _db.StoredFiles.Remove(file);
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
