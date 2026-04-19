namespace DataShare.Application.Services;

public abstract record DownloadResult
{
    public sealed record Success(Stream Stream, string FileName, string ContentType, long Size) : DownloadResult;
    public sealed record Failure(DownloadError Error) : DownloadResult;
}
