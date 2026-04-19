namespace DataShare.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
