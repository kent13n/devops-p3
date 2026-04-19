using DataShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataShare.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<StoredFile> StoredFiles { get; }
    DbSet<Tag> Tags { get; }
    DbSet<FileTag> FileTags { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
