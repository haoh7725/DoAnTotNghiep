using System.ComponentModel.DataAnnotations;
using ResearchManagement.Application.Common;

namespace ResearchManagement.Application.Products.Models;

// ──── Requests ────────────────────────────────────────────────────────────────

public sealed class ProductSearchQuery : PagedQuery
{
    /// <summary>Tìm theo tên, mã sản phẩm, tên tạp chí/hội nghị hoặc DOI, không phân biệt hoa thường.</summary>
    [StringLength(100)]
    public string? Keyword { get; set; }

    [Range(1, long.MaxValue)] public long? ProductTypeId { get; set; }
    [Range(1, long.MaxValue)] public long? AcademicYearId { get; set; }

    [RegularExpression("^(NHAP|CHO_DUYET|CAN_BO_SUNG|DA_DUYET|TU_CHOI)$", ErrorMessage = "Trạng thái xét duyệt không hợp lệ.")]
    public string? ReviewStatus { get; set; }

    [RegularExpression("^(DANG_VIET|DANG_PHAN_BIEN|DA_NHAN_XET|DA_XUAT_BAN)$", ErrorMessage = "Trạng thái bài báo không hợp lệ.")]
    public string? ArticleStatus { get; set; }

    /// <summary>Lọc theo một giảng viên tham gia (tác giả hoặc đồng tác giả).</summary>
    [Range(1, long.MaxValue)] public long? LecturerId { get; set; }

    /// <summary>Lọc theo khoa/bộ môn ghi nhận của các tác giả.</summary>
    [Range(1, long.MaxValue)] public long? FacultyId { get; set; }
    [Range(1, long.MaxValue)] public long? DepartmentId { get; set; }

    /// <summary>Chỉ lấy sản phẩm do tôi tạo hoặc tôi là tác giả.</summary>
    public bool Mine { get; set; }
}

/// <summary>Các trường nội dung dùng chung khi thêm và sửa sản phẩm. Trường nào áp dụng tùy loại sản phẩm.</summary>
public class ProductContentRequest
{
    [Range(1, long.MaxValue)] public long AcademicYearId { get; set; }

    [Required, StringLength(500, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Range(1900, 2200, ErrorMessage = "Năm công bố phải từ 1900 đến 2200.")]
    public int? PublicationYear { get; set; }

    [StringLength(4000)] public string? PublicationInfo { get; set; }
    [StringLength(255)] public string? Doi { get; set; }
    [StringLength(30)] public string? Isbn { get; set; }
    [StringLength(20)] public string? Issn { get; set; }
    [StringLength(300)] public string? JournalName { get; set; }
    [StringLength(100)] public string? JournalIndex { get; set; }
    [StringLength(100)] public string? JournalCategory { get; set; }

    [Range(0, 999999.9999)] public decimal? WorkScore { get; set; }

    [StringLength(300)] public string? ResearchField { get; set; }
    [StringLength(300)] public string? Publisher { get; set; }
    [StringLength(100)] public string? ProjectLevel { get; set; }
    [StringLength(300)] public string? HostUnit { get; set; }
    [StringLength(4000)] public string? ProjectObjective { get; set; }
    [StringLength(4000)] public string? ProjectContent { get; set; }
    [StringLength(4000)] public string? ExpectedResult { get; set; }
    [StringLength(100)] public string? CertificateNumber { get; set; }
    [StringLength(300)] public string? IssuingAuthority { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? SubmittedDate { get; set; }
    public DateOnly? PublishedDate { get; set; }
}

public sealed class UpdateProductRequest : ProductContentRequest;

public sealed class CreateProductRequest : ProductContentRequest
{
    [Range(1, long.MaxValue)] public long ProductTypeId { get; set; }

    /// <summary>Chỉ dùng cho loại BAI_BAO. Bỏ trống thì mặc định DANG_VIET.</summary>
    [RegularExpression("^(DANG_VIET|DANG_PHAN_BIEN|DA_NHAN_XET|DA_XUAT_BAN)$", ErrorMessage = "Trạng thái bài báo không hợp lệ.")]
    public string? ArticleStatus { get; set; }

    /// <summary>
    /// Tác giả chính. Bỏ trống = giảng viên của tài khoản đang đăng nhập. Chỉ Phòng QLKH/Quản trị
    /// được chọn người khác (nhập hộ).
    /// </summary>
    [Range(1, long.MaxValue)] public long? MainAuthorLecturerId { get; set; }

    /// <summary>Đồng tác giả ban đầu, theo thứ tự (tác giả chính luôn đứng đầu).</summary>
    [MaxLength(29)] public List<ProductAuthorInput>? CoAuthors { get; set; }
}

public sealed class ChangeArticleStatusRequest
{
    [Required, RegularExpression("^(DANG_VIET|DANG_PHAN_BIEN|DA_NHAN_XET|DA_XUAT_BAN)$", ErrorMessage = "Trạng thái bài báo không hợp lệ.")]
    public string Status { get; set; } = string.Empty;
}

public class ProductAuthorInput
{
    [Range(1, long.MaxValue)] public long LecturerId { get; set; }

    /// <summary>TAC_GIA_CHINH, TAC_GIA_LIEN_HE hoặc DONG_TAC_GIA.</summary>
    [Required, RegularExpression("^(TAC_GIA_CHINH|TAC_GIA_LIEN_HE|DONG_TAC_GIA)$", ErrorMessage = "Vai trò tác giả không hợp lệ.")]
    public string Role { get; set; } = "DONG_TAC_GIA";
}

/// <summary>Toàn bộ danh sách tác giả sau khi sửa; thứ tự trong mảng là thứ tự tác giả.</summary>
public sealed class ReplaceProductAuthorsRequest
{
    [Required, MinLength(1), MaxLength(30)]
    public List<ProductAuthorInput> Authors { get; set; } = [];
}

/// <summary>Thêm một đồng tác giả vào cuối danh sách.</summary>
public sealed class AddProductAuthorRequest : ProductAuthorInput;

// ──── Responses ───────────────────────────────────────────────────────────────

public sealed record ProductAuthorResponse(
    long LecturerId,
    long AccountId,
    string LecturerCode,
    string LecturerName,
    string Role,
    int Order,
    long DepartmentId,
    string DepartmentName,
    long FacultyId);

/// <summary>Thông tin tối thiểu của giảng viên để chọn làm tác giả; không lộ liên hệ hay thông tin cá nhân.</summary>
public sealed record AuthorCandidateResponse(
    long Id, string Code, string FullName, string DepartmentName, string FacultyName);

public sealed record ProductAuthorBrief(long LecturerId, string LecturerName, string Role, int Order);

public sealed record ProductSummaryResponse(
    long Id,
    string Code,
    string Title,
    long ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    long AcademicYearId,
    string AcademicYearCode,
    int? PublicationYear,
    string? JournalName,
    string? ArticleStatus,
    string ReviewStatus,
    int Version,
    IReadOnlyList<ProductAuthorBrief> Authors);

/// <summary>Các thao tác người dùng hiện tại được làm, để giao diện không tự suy luận lại quy tắc.</summary>
public sealed record ProductPermissionsResponse(
    bool CanEdit,
    bool CanManageAuthors,
    bool CanChangeArticleStatus,
    IReadOnlyList<string> NextArticleStatuses);

public sealed record ProductDetailResponse(
    long Id,
    string Code,
    string Title,
    long ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    long AcademicYearId,
    string AcademicYearCode,
    long CreatedByAccountId,
    string CreatedByName,
    int? PublicationYear,
    string? PublicationInfo,
    string? Doi,
    string? Isbn,
    string? Issn,
    string? JournalName,
    string? JournalIndex,
    string? JournalCategory,
    decimal? WorkScore,
    string? ResearchField,
    string? Publisher,
    string? ProjectLevel,
    string? HostUnit,
    string? ProjectObjective,
    string? ProjectContent,
    string? ExpectedResult,
    string? CertificateNumber,
    string? IssuingAuthority,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateOnly? SubmittedDate,
    DateOnly? PublishedDate,
    string? ArticleStatus,
    string ReviewStatus,
    int Version,
    IReadOnlyList<ProductAuthorResponse> Authors,
    ProductPermissionsResponse? Permissions);

/// <summary>Một tác giả sẽ có mặt trong sản phẩm sau khi lưu.</summary>
public sealed record AuthorSlot(long LecturerId, long DepartmentId, string Role, int Order);

/// <summary>Phạm vi sản phẩm mà người dùng hiện tại được liệt kê, suy ra từ phân quyền.</summary>
public sealed record ProductVisibility(
    bool IsAdmin,
    bool All,
    IReadOnlyCollection<long> FacultyIds,
    IReadOnlyCollection<long> DepartmentIds,
    long? AccountId);

// ──── Giữ nguyên cho phần minh chứng và xét duyệt (các tuần sau) ───────────────

public sealed record EvidenceResponse(
    long Id,
    long ProductId,
    long UploadedByAccountId,
    string UploadedByName,
    string FileName,
    string MimeType,
    long FileSizeBytes,
    string? Sha256,
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
