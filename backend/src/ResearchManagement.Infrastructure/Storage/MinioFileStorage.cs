using Minio;
using Minio.DataModel.Args;
using ResearchManagement.Application.Products.Abstractions;

namespace ResearchManagement.Infrastructure.Storage;

public sealed class MinioFileStorage(IMinioClient client, MinioOptions options) : IFileStorage
{
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketReady;

    public async Task PutAsync(
        string objectKey,
        Stream content,
        long size,
        string contentType,
        CancellationToken cancellationToken)
    {
        await EnsureBucketAsync(cancellationToken);
        await client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(options.BucketName)
                .WithObject(objectKey)
                .WithStreamData(content)
                .WithObjectSize(size)
                .WithContentType(contentType),
            cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        await EnsureBucketAsync(cancellationToken);
        var output = new MemoryStream();
        try
        {
            await client.GetObjectAsync(new GetObjectArgs()
                    .WithBucket(options.BucketName)
                    .WithObject(objectKey)
                    .WithCallbackStream(stream => stream.CopyTo(output)),
                cancellationToken);
            output.Position = 0;
            return output;
        }
        catch
        {
            await output.DisposeAsync();
            throw;
        }
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        await EnsureBucketAsync(cancellationToken);
        await client.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(options.BucketName)
                .WithObject(objectKey),
            cancellationToken);
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketReady) return;

        await _bucketLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketReady) return;
            var exists = await client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(options.BucketName), cancellationToken);
            if (!exists)
                await client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(options.BucketName), cancellationToken);
            _bucketReady = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }
}
