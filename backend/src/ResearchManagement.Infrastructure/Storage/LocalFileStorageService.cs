using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using ResearchManagement.Application.Common;

namespace ResearchManagement.Infrastructure.Storage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredPath = configuration["Storage:RootPath"];
        _rootPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.GetFullPath(Path.Combine(environment.ContentRootPath, "uploads"))
            : Path.GetFullPath(configuredPath);

        if (!Directory.Exists(_rootPath))
            Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        string subDirectory,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var cleanSub = subDirectory.Trim().Replace('\\', '/').Trim('/');
        var cleanName = Path.GetFileName(fileName);
        var relativeKey = string.IsNullOrEmpty(cleanSub) ? cleanName : $"{cleanSub}/{cleanName}";

        var fullPath = ResolveSafePath(relativeKey);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(fileStream, cancellationToken);
        return relativeKey;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(storageKey);
        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(storageKey);
        return Task.FromResult(File.Exists(fullPath));
    }

    private string ResolveSafePath(string relativeKey)
    {
        var normalizedKey = relativeKey.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalizedKey));

        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("Đường dẫn lưu trữ tệp không hợp lệ hoặc vượt ra ngoài thư mục cho phép.");

        return fullPath;
    }
}
