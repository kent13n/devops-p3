namespace DataShare.Application.DTOs;

public record FileDto(
    Guid Id,
    string OriginalName,
    long SizeBytes,
    string MimeType,
    string DownloadToken,
    string DownloadUrl,
    DateTime ExpiresAt,
    bool IsProtected,
    string[] Tags,
    DateTime CreatedAt);
