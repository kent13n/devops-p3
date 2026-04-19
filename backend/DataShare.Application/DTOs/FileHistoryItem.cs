namespace DataShare.Application.DTOs;

public record FileHistoryItem(
    Guid Id,
    string OriginalName,
    long SizeBytes,
    string MimeType,
    string? DownloadUrl,
    DateTime ExpiresAt,
    bool IsExpired,
    bool IsPurged,
    bool IsProtected,
    string[] Tags,
    DateTime CreatedAt);
