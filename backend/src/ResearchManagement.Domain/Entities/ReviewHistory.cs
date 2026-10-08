using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>
/// Bảng nckh.lich_su_xet_duyet.
/// Ghi lại mỗi lần chuyển trạng thái xét duyệt của sản phẩm (append-only).
/// </summary>
public sealed class ReviewHistory : BaseEntity
{
    private ReviewHistory() { }

    public ReviewHistory(
        long productId,
        long actorAccountId,
        string fromStatus,
        string toStatus,
        string? comment)
    {
        ProductId = productId;
        ActorAccountId = actorAccountId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Comment = comment?.Trim();
        OccurredAt = DateTimeOffset.UtcNow;
    }

    /// <summary>FK → san_pham.id</summary>
    public long ProductId { get; private set; }

    /// <summary>FK → tai_khoan.id (người thực hiện chuyển trạng thái)</summary>
    public long ActorAccountId { get; private set; }

    public string FromStatus { get; private set; } = string.Empty;
    public string ToStatus { get; private set; } = string.Empty;

    /// <summary>Ghi chú hoặc lý do trả lại.</summary>
    public string? Comment { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
}
