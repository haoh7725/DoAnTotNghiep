namespace ResearchManagement.Application.Products.Abstractions;

public interface IFileStorage
{
    Task PutAsync(
        string objectKey,
        Stream content,
        long size,
        string contentType,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
