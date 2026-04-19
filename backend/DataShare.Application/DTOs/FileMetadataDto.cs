namespace DataShare.Application.DTOs;

public record FileMetadataDto(
    string OriginalName,
    long SizeBytes,
    string MimeType,
    DateTime ExpiresAt,
    bool IsProtected);
