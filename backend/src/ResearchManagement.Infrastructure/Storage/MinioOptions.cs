using Microsoft.Extensions.Configuration;

namespace ResearchManagement.Infrastructure.Storage;

public sealed record MinioOptions(
    string Endpoint,
    string AccessKey,
    string SecretKey,
    string BucketName,
    bool UseSsl)
{
    public static MinioOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection("Minio");
        return new MinioOptions(
            section["Endpoint"] ?? throw new InvalidOperationException("Thiếu cấu hình Minio:Endpoint."),
            section["AccessKey"] ?? throw new InvalidOperationException("Thiếu cấu hình Minio:AccessKey."),
            section["SecretKey"] ?? throw new InvalidOperationException("Thiếu cấu hình Minio:SecretKey."),
            section["BucketName"] ?? "research-evidences",
            section.GetValue("UseSsl", false));
    }
}
