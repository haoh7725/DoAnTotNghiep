namespace ResearchManagement.Application.Common;

public interface IFileStorageService
{
    Task<string> SaveAsync(string subDirectory, string fileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);
}
