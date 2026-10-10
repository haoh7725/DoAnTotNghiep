
using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng thong_bao.</summary>
public sealed class Notification : BaseEntity
{
    private Notification() { }

    public Notification(
        long recipientAccountId,
        string type,
        string content,
        string deduplicationKey,
        long? researchPlanItemId = null,
        long? feedbackId = null,
        long? researchPlanId = null,
        long? scientificProductId = null,
        DateTimeOffset? scheduledAt = null)
    {
        if (recipientAccountId <= 0)
            throw new ArgumentOutOfRangeException(nameof(recipientAccountId));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Nội dung thông báo không được trống.");

        if (string.IsNullOrWhiteSpace(deduplicationKey))
            throw new ArgumentException("Khóa chống trùng không được trống.");

        if (deduplicationKey.Length > 191)
            throw new ArgumentException("Khóa chống trùng tối đa 191 ký tự.");

        if (type is not ("NHAC_HAN" or "QUA_HAN" or "PHAN_HOI" or "HE_THONG"))
            throw new ArgumentException("Loại thông báo không hợp lệ.");

        RecipientAccountId = recipientAccountId;
        Type = type;
        Content = content;
        DeduplicationKey = deduplicationKey;
        ResearchPlanItemId = researchPlanItemId;
        FeedbackId = feedbackId;
        ResearchPlanId = researchPlanId;
        ScientificProductId = scientificProductId;

        Channel = "IN_APP";
        DeliveryStatus = "CHO_GUI";
        RetryCount = 0;
        ScheduledAt = scheduledAt ?? DateTimeOffset.UtcNow;
    }

    public long RecipientAccountId { get; private set; }
    public long? ResearchPlanItemId { get; private set; }
    public long? FeedbackId { get; private set; }
    public long? ResearchPlanId { get; private set; }
    public long? ScientificProductId { get; private set; }

    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string DeduplicationKey { get; private set; } = string.Empty;

    public string Channel { get; private set; } = "IN_APP";
    public string DeliveryStatus { get; private set; } = "CHO_GUI";
    public int RetryCount { get; private set; }

    public DateTimeOffset ScheduledAt { get; private set; }
    public DateTimeOffset? DeliveredUntil { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public string? DeliveryError { get; private set; }

    public bool IsRead => ReadAt.HasValue;

    public void MarkAsRead()
    {
        if (!ReadAt.HasValue)
            ReadAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsSent()
    {
        DeliveryStatus = "DA_GUI";
        SentAt = DateTimeOffset.UtcNow;
        DeliveryError = null;
    }

    public void MarkAsFailed(string error)
    {
        DeliveryStatus = "LOI";
        RetryCount++;
        DeliveryError = error;
    }
}
