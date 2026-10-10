using ResearchManagement.Domain.Common;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Domain.Entities;

/// <summary>Các trường nội dung do người dùng nhập của một sản phẩm khoa học.</summary>
public sealed record ProductContent(
    string Title,
    short? PublicationYear,
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
    DateOnly? PublishedDate);

/// <summary>
/// Bảng san_pham_khoa_hoc: bài báo, đề tài, sách, chứng nhận. Loại sản phẩm quyết định các trường được dùng.
/// Trạng thái bài báo (ArticleStatus) và trạng thái xét duyệt (ReviewStatus) là hai khái niệm tách biệt.
/// </summary>
public sealed class Product : BaseEntity
{
    private Product() { }

    public Product(
        long productTypeId, long academicYearId, long createdByAccountId,
        string code, ProductContent content, string? articleStatus)
    {
        ProductTypeId = productTypeId;
        AcademicYearId = academicYearId;
        CreatedByAccountId = createdByAccountId;
        Code = code;
        ArticleStatus = articleStatus;
        ReviewStatus = ReviewStatuses.Draft;
        Version = 1;
        ApplyContent(content);
    }

    public long ProductTypeId { get; private set; }
    public long AcademicYearId { get; private set; }
    public long CreatedByAccountId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public short? PublicationYear { get; private set; }
    public string? PublicationInfo { get; private set; }
    public string? Doi { get; private set; }
    public string? Isbn { get; private set; }
    public string? Issn { get; private set; }
    public string? JournalName { get; private set; }
    public string? JournalIndex { get; private set; }
    public string? JournalCategory { get; private set; }
    public decimal? WorkScore { get; private set; }
    public string? ResearchField { get; private set; }
    public string? Publisher { get; private set; }
    public string? ProjectLevel { get; private set; }
    public string? HostUnit { get; private set; }
    public string? ProjectObjective { get; private set; }
    public string? ProjectContent { get; private set; }
    public string? ExpectedResult { get; private set; }
    public string? CertificateNumber { get; private set; }
    public string? IssuingAuthority { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateOnly? SubmittedDate { get; private set; }
    public DateOnly? PublishedDate { get; private set; }

    /// <summary>Tiến độ bài báo; null với các loại sản phẩm khác BAI_BAO.</summary>
    public string? ArticleStatus { get; private set; }

    /// <summary>Trạng thái xét duyệt; do luồng gửi duyệt (tuần 3) thay đổi, không sửa trực tiếp ở đây.</summary>
    public string ReviewStatus { get; private set; } = ReviewStatuses.Draft;

    /// <summary>Số phiên bản đã gửi duyệt; luồng gửi duyệt quản lý, bắt đầu từ 1.</summary>
    public int Version { get; private set; } = 1;

    public void Update(long academicYearId, ProductContent content)
    {
        AcademicYearId = academicYearId;
        ApplyContent(content);
    }

    public void ChangeArticleStatus(string status) => ArticleStatus = status;

    private void ApplyContent(ProductContent c)
    {
        Title = c.Title;
        PublicationYear = c.PublicationYear;
        PublicationInfo = c.PublicationInfo;
        Doi = c.Doi;
        Isbn = c.Isbn;
        Issn = c.Issn;
        JournalName = c.JournalName;
        JournalIndex = c.JournalIndex;
        JournalCategory = c.JournalCategory;
        WorkScore = c.WorkScore;
        ResearchField = c.ResearchField;
        Publisher = c.Publisher;
        ProjectLevel = c.ProjectLevel;
        HostUnit = c.HostUnit;
        ProjectObjective = c.ProjectObjective;
        ProjectContent = c.ProjectContent;
        ExpectedResult = c.ExpectedResult;
        CertificateNumber = c.CertificateNumber;
        IssuingAuthority = c.IssuingAuthority;
        StartDate = c.StartDate;
        EndDate = c.EndDate;
        SubmittedDate = c.SubmittedDate;
        PublishedDate = c.PublishedDate;
    }
}
