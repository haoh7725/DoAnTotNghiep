using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

public sealed class ProductFeedback : BaseEntity
{
    private ProductFeedback() { }
    public ProductFeedback(long productId, long senderAccountId, string content)
    {
        if (productId <= 0 || senderAccountId <= 0) throw new ArgumentOutOfRangeException();
        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Nội dung phản hồi không được trống.");
        ProductId = productId; SenderAccountId = senderAccountId;
        Content = content.Trim(); CreatedAt = DateTimeOffset.UtcNow;
    }
    public long ProductId { get; private set; }
    public long SenderAccountId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
