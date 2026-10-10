using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>
/// Bảng nckh.minh_chung.
/// Tệp minh chứng đính kèm sản phẩm NCKH, lưu trữ an toàn trên server.
/// </summary>
public sealed class ProductEvidence : BaseEntity
{
    private ProductEvidence() { }

    public ProductEvidence(
        long productId,
        long uploadedByAccountId,
        string fileName,
        string storageKey,
        string mimeType,
        long fileSizeBytes,
        string? sha256 = null)
    {
        ProductId = productId;
        UploadedByAccountId = uploadedByAccountId;
        FileName = fileName.Trim();
        StorageKey = storageKey;
        MimeType = mimeType;
        FileSizeBytes = fileSizeBytes;
        Sha256 = sha256;
        UploadedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>FK → san_pham_khoa_hoc.id (san_pham_id)</summary>
    public long ProductId { get; private set; }

    /// <summary>FK → tai_khoan.id (nguoi_tai_id)</summary>
    public long UploadedByAccountId { get; private set; }

    /// <summary>Tên file/tài liệu gốc khi upload (ten_tai_lieu).</summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>Khóa lưu trữ / đường dẫn tương đối (khoa_luu_tru).</summary>
    public string StorageKey { get; private set; } = string.Empty;

    /// <summary>Định dạng MIME của tệp (mime_type).</summary>
    public string MimeType { get; private set; } = string.Empty;

    /// <summary>Dung lượng tệp tính bằng byte (kich_thuoc).</summary>
    public long FileSizeBytes { get; private set; }

    /// <summary>Mã băm SHA256 để kiểm tra tính toàn vẹn (sha256).</summary>
    public string? Sha256 { get; private set; }

    /// <summary>Thời điểm tải lên (tai_luc).</summary>
    public DateTimeOffset UploadedAt { get; private set; }
}
