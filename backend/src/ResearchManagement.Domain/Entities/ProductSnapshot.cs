namespace ResearchManagement.Domain.Entities;

/// <summary>
/// Bảng nckh.san_pham_phien_ban.
/// Lưu ảnh chụp nội dung sản phẩm mỗi lần nộp hoặc chuyển trạng thái.
/// </summary>
public sealed class ProductSnapshot
{
    private ProductSnapshot() { }

    public ProductSnapshot(Product product, long snapshotByAccountId, int version)
    {
        ProductId = product.Id;
        Version = version;
        SnapshotByAccountId = snapshotByAccountId;
        SnapshotAt = DateTimeOffset.UtcNow;
        // Copy all content fields from product
        Title = product.Title;
        Description = product.Description;
        PublicationInfo = product.PublicationInfo;
        PublishedDate = product.PublishedDate;
        ArticleStatus = product.ArticleStatus;
        JournalIndex = product.JournalIndex;
        JournalClassification = product.JournalClassification;
        ProjectLevel = product.ProjectLevel;
        Status = product.Status;
    }

    public long ProductId { get; private set; }
    public int Version { get; private set; }
    public long SnapshotByAccountId { get; private set; }
    public DateTimeOffset SnapshotAt { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? PublicationInfo { get; private set; }
    public DateOnly? PublishedDate { get; private set; }
    public string? ArticleStatus { get; private set; }
    public string? JournalIndex { get; private set; }
    public string? JournalClassification { get; private set; }
    public string? ProjectLevel { get; private set; }
    public string Status { get; private set; } = string.Empty;
}
