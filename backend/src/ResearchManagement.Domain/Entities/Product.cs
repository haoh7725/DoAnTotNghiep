using ResearchManagement.Domain.Common;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Domain.Entities;

/// <summary>
/// Bảng nckh.san_pham.
/// Sản phẩm NCKH thực tế mà giảng viên nộp, liên kết với một nội dung kế hoạch.
/// </summary>
public sealed class Product : BaseEntity
{
    private Product() { }

    public Product(
        long researchPlanItemId,
        long submittedByLecturerId,
        string title,
        string? description,
        string? publicationInfo,
        DateOnly? publishedDate)
    {
        ResearchPlanItemId = researchPlanItemId;
        SubmittedByLecturerId = submittedByLecturerId;
        Title = title.Trim();
        Description = description?.Trim();
        PublicationInfo = publicationInfo?.Trim();
        PublishedDate = publishedDate;
        Status = ProductStatuses.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>FK → noi_dung_ke_hoach.id</summary>
    public long ResearchPlanItemId { get; private set; }

    /// <summary>FK → giang_vien.id (tác giả chính)</summary>
    public long SubmittedByLecturerId { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>Nơi đăng / tên tạp chí / kỷ yếu / nhà xuất bản.</summary>
    public string? PublicationInfo { get; private set; }

    public DateOnly? PublishedDate { get; private set; }

    public string Status { get; private set; } = ProductStatuses.Draft;

    /// <summary>Điểm quy đổi do Phòng QLKH nhập khi duyệt cuối.</summary>
    public decimal? ScoreEquivalent { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // ──── Hành vi ────────────────────────────────────────────────────────────

    /// <summary>Giảng viên cập nhật nội dung khi đang ở trạng thái NHAP hoặc TRA_LAI.</summary>
    public void Update(
        string title,
        string? description,
        string? publicationInfo,
        DateOnly? publishedDate)
    {
        Title = title.Trim();
        Description = description?.Trim();
        PublicationInfo = publicationInfo?.Trim();
        PublishedDate = publishedDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Giảng viên nộp sản phẩm để xét duyệt.</summary>
    public void Submit()
    {
        Status = ProductStatuses.Submitted;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Chuyển trạng thái sau khi được duyệt qua một cấp.</summary>
    public void Approve(string nextStatus, decimal? scoreEquivalent = null)
    {
        Status = nextStatus;
        if (scoreEquivalent.HasValue)
            ScoreEquivalent = scoreEquivalent;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Trả lại để giảng viên chỉnh sửa; trạng thái về NHAP.</summary>
    public void Return()
    {
        Status = ProductStatuses.Draft;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
