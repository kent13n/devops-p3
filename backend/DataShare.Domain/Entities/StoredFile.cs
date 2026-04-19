namespace DataShare.Domain.Entities;

public class StoredFile
{
    public Guid Id { get; set; }
    public Guid? OwnerId { get; set; }
    public string OriginalName { get; set; } = default!;
    public string MimeType { get; set; } = default!;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = default!;
    public string DownloadToken { get; set; } = default!;
    public string? PasswordHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<FileTag> FileTags { get; set; } = [];
}
