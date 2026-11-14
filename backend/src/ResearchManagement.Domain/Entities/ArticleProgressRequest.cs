using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Đề nghị của tác giả chuyển một bước tiến độ công bố bài báo.</summary>
public sealed class ArticleProgressRequest : BaseEntity
{
    private ArticleProgressRequest() { }

    public ArticleProgressRequest(long productId, long requestedByAccountId, string fromStatus,
        string toStatus, long evidenceId, string requiredRole, DateOnly? publishedDate,
        string? publicationInfo, string? note)
    {
        ProductId = productId;
        RequestedByAccountId = requestedByAccountId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        EvidenceId = evidenceId;
        RequiredRole = requiredRole;
        PublishedDate = publishedDate;
        PublicationInfo = publicationInfo?.Trim();
        Note = note?.Trim();
        Status = "CHO_DUYET";
        RequestedAt = DateTimeOffset.UtcNow;
    }

    public long ProductId { get; private set; }
    public long RequestedByAccountId { get; private set; }
    public string FromStatus { get; private set; } = string.Empty;
    public string ToStatus { get; private set; } = string.Empty;
    public long EvidenceId { get; private set; }
    public string RequiredRole { get; private set; } = string.Empty;
    public DateOnly? PublishedDate { get; private set; }
    public string? PublicationInfo { get; private set; }
    public string? Note { get; private set; }
    public string Status { get; private set; } = "CHO_DUYET";
    public long? ReviewedByAccountId { get; private set; }
    public string? ReviewComment { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    public void Approve(long reviewerAccountId, string? comment)
    {
        EnsurePending();
        Status = "DA_DUYET";
        ReviewedByAccountId = reviewerAccountId;
        ReviewComment = comment?.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(long reviewerAccountId, string comment)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(comment))
            throw new InvalidOperationException("Phải nhập lý do trả lại đề nghị.");
        Status = "TRA_LAI";
        ReviewedByAccountId = reviewerAccountId;
        ReviewComment = comment.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != "CHO_DUYET")
            throw new InvalidOperationException("Đề nghị đã được xử lý.");
    }
}
