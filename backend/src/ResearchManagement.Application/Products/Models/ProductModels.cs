using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Application.Products.Models;

// ──── Requests ────────────────────────────────────────────────────────────────

public sealed class CreateProductRequest
{
    [Required]
    public long ResearchPlanItemId { get; set; }

    [Required, StringLength(500, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [StringLength(500)]
    public string? PublicationInfo { get; set; }

    public DateOnly? PublishedDate { get; set; }
    [RegularExpression("^(DANG_VIET|DANG_PHAN_BIEN|DA_NHAN_DANG|DA_XUAT_BAN)$")] public string? ArticleStatus { get; set; }
    [StringLength(100)] public string? JournalIndex { get; set; }
    [StringLength(100)] public string? JournalClassification { get; set; }
    [StringLength(100)] public string? ProjectLevel { get; set; }
}

public sealed class UpdateProductRequest
{
    [Required, StringLength(500, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [StringLength(500)]
    public string? PublicationInfo { get; set; }

    public DateOnly? PublishedDate { get; set; }
    [RegularExpression("^(DANG_VIET|DANG_PHAN_BIEN|DA_NHAN_DANG|DA_XUAT_BAN)$")] public string? ArticleStatus { get; set; }
    [StringLength(100)] public string? JournalIndex { get; set; }
    [StringLength(100)] public string? JournalClassification { get; set; }
    [StringLength(100)] public string? ProjectLevel { get; set; }
}

public sealed class ReviewProductRequest
{
    /// <summary>"APPROVE" hoặc "REJECT".</summary>
    [Required]
    public string Action { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Comment { get; set; }

    /// <summary>Điểm quy đổi — chỉ Phòng QLKH nhập khi duyệt cuối.</summary>
    [Range(0, 999.99)]
    public decimal? ScoreEquivalent { get; set; }
}

public sealed class AddCoAuthorRequest
{
    [Required]
    public long LecturerId { get; set; }

    [Range(1, int.MaxValue)]
    public int DisplayOrder { get; set; } = 1;
    [Required, StringLength(100)] public string AuthorRole { get; set; } = "DONG_TAC_GIA";
}

public sealed class UpdateCoAuthorRequest
{
    [Range(1, int.MaxValue)]
    public int DisplayOrder { get; set; }
    [Required, StringLength(100)] public string AuthorRole { get; set; } = "DONG_TAC_GIA";
}

// ──── Responses ───────────────────────────────────────────────────────────────

public sealed record CoAuthorResponse(
    long Id,
    long ProductId,
    long LecturerId,
    string LecturerFullName,
    string? LecturerCode,
    int DisplayOrder,
    string AuthorRole);

public sealed record EvidenceResponse(
    long Id,
    long ProductId,
    string OriginalFileName,
    long FileSizeBytes,
    string? Description,
    DateTimeOffset UploadedAt);

public sealed record ReviewHistoryResponse(
    long Id,
    long ProductId,
    long ActorAccountId,
    string ActorUsername,
    string FromStatus,
    string ToStatus,
    string? Comment,
    DateTimeOffset OccurredAt);

public sealed record ProductResponse(
    long Id,
    long ResearchPlanItemId,
    long SubmittedByLecturerId,
    string SubmittedByLecturerFullName,
    string Title,
    string? Description,
    string? PublicationInfo,
    DateOnly? PublishedDate,
    string? ArticleStatus,
    string? JournalIndex,
    string? JournalClassification,
    string? ProjectLevel,
    string Status,
    decimal? ScoreEquivalent,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<CoAuthorResponse> CoAuthors,
    IReadOnlyList<EvidenceResponse> Evidences);

public sealed record ProductSummaryResponse(
    long Id,
    long ResearchPlanItemId,
    string Title,
    string? ArticleStatus,
    string Status,
    decimal? ScoreEquivalent,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
