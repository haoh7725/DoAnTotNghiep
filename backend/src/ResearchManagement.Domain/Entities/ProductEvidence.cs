using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>
/// Bảng nckh.minh_chung.
/// Tệp minh chứng đính kèm sản phẩm NCKH, lưu local trên server.
/// </summary>
public sealed class ProductEvidence : BaseEntity
{
    private ProductEvidence() { }

    public ProductEvidence(
        long productId,
        long uploadedByAccountId,
        string originalFileName,
        string storedPath,
        long fileSizeBytes,
        string? description)
    {
        ProductId = productId;
        UploadedByAccountId = uploadedByAccountId;
        OriginalFileName = originalFileName.Trim();
        StoredPath = storedPath;
        FileSizeBytes = fileSizeBytes;
        Description = description?.Trim();
        UploadedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>FK → san_pham.id</summary>
    public long ProductId { get; private set; }

    /// <summary>FK → tai_khoan.id (người upload)</summary>
    public long UploadedByAccountId { get; private set; }

    /// <summary>Tên file gốc khi upload (để hiển thị).</summary>
    public string OriginalFileName { get; private set; } = string.Empty;

    /// <summary>Đường dẫn tương đối trong thư mục uploads/ trên server.</summary>
    public string StoredPath { get; private set; } = string.Empty;

    public long FileSizeBytes { get; private set; }

    /// <summary>Mô tả ngắn gọn về tệp minh chứng.</summary>
    public string? Description { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }
}
