using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Application.Lecturers.Models;

/// <summary>Các mục bổ sung theo Phụ lục V Thông tư 08/2011/TT-BGDĐT.
/// Họ tên, giới tính, ngày sinh, học vị, học hàm, chức vụ, email, di động lấy từ hồ sơ giảng viên.</summary>
public sealed class ScientificProfileDetails
{
    [StringLength(200)] public string? BirthPlace { get; set; }
    [StringLength(200)] public string? Hometown { get; set; }
    [StringLength(100)] public string? Ethnicity { get; set; }
    [Range(1900, 2100)] public int? HighestDegreeYear { get; set; }
    [StringLength(100)] public string? HighestDegreeCountry { get; set; }
    [Range(1900, 2100)] public int? AcademicRankYear { get; set; }
    [StringLength(300)] public string? WorkUnit { get; set; }
    [StringLength(500)] public string? ContactAddress { get; set; }
    [StringLength(30)] public string? OfficePhone { get; set; }
    [StringLength(30)] public string? HomePhone { get; set; }
    [StringLength(30)] public string? Fax { get; set; }
    [StringLength(30)] public string? IdentityNumber { get; set; }
    public DateOnly? IdentityIssuedDate { get; set; }
    [StringLength(200)] public string? IdentityIssuedPlace { get; set; }
    [MaxLength(100)] public List<EducationEntry> Education { get; set; } = [];
    [MaxLength(100)] public List<LanguageEntry> Languages { get; set; } = [];
    [MaxLength(100)] public List<EmploymentEntry> Employment { get; set; } = [];
    [MaxLength(500)] public List<ResearchProjectEntry> ResearchProjects { get; set; } = [];
    [MaxLength(500)] public List<PublicationEntry> Publications { get; set; } = [];
    [StringLength(200)] public string? DeclarationPlace { get; set; }
    public DateOnly? DeclarationDate { get; set; }

    public void Validate(DateOnly today)
    {
        ValidateObject(this);
        if (Education is null || Languages is null || Employment is null || ResearchProjects is null || Publications is null)
            throw new ValidationException("Các danh sách lý lịch phải là mảng, không được là null.");
        foreach (var entry in Education) ValidateObject(entry);
        foreach (var entry in Languages) ValidateObject(entry);
        foreach (var entry in Employment) ValidateObject(entry);
        foreach (var entry in ResearchProjects) ValidateObject(entry);
        foreach (var entry in Publications) ValidateObject(entry);
        if (HighestDegreeYear > today.Year || AcademicRankYear > today.Year ||
            Education.Any(e => e.GraduationYear > today.Year) || Publications.Any(p => p.Year > today.Year))
            throw new ValidationException("Năm nhận học vị, bổ nhiệm, tốt nghiệp và công bố không được ở tương lai.");
        if (IdentityIssuedDate > today || DeclarationDate > today)
            throw new ValidationException("Ngày cấp giấy tờ và ngày kê khai không được ở tương lai.");
        if (Employment.Any(e => e.FromDate > today || e.ToDate > today || e.FromDate > e.ToDate))
            throw new ValidationException("Thời gian công tác không hợp lệ: ngày kết thúc phải từ ngày bắt đầu và không ở tương lai.");
        if (ResearchProjects.Any(p => p.StartYear > today.Year || p.StartYear > p.EndYear))
            throw new ValidationException("Năm bắt đầu đề tài không được ở tương lai hoặc sau năm hoàn thành.");
    }

    private static void ValidateObject(object? value)
    {
        if (value is null) throw new ValidationException("Dòng thông tin không được là null.");
        Validator.ValidateObject(value, new ValidationContext(value), validateAllProperties: true);
    }
}

public sealed class EducationEntry
{
    [Required, RegularExpression("^(DAI_HOC|DAI_HOC_2|THAC_SI|TIEN_SI)$")]
    public string Level { get; set; } = "DAI_HOC";
    [StringLength(100)] public string? TrainingMode { get; set; }
    [Required, StringLength(300)] public string Institution { get; set; } = "";
    [Required, StringLength(200)] public string Specialization { get; set; } = "";
    [StringLength(100)] public string? Country { get; set; }
    [Range(1900, 2100)] public int? GraduationYear { get; set; }
    [StringLength(2000)] public string? ThesisTitle { get; set; }
}

public sealed class LanguageEntry
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(300)] public string Proficiency { get; set; } = "";
}

public sealed class EmploymentEntry
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    [Required, StringLength(300)] public string Organization { get; set; } = "";
    [Required, StringLength(1000)] public string Position { get; set; } = "";
}

public sealed class ResearchProjectEntry
{
    [Required, StringLength(1000)] public string Title { get; set; } = "";
    [Range(1900, 2100)] public int? StartYear { get; set; }
    [Range(1900, 2100)] public int? EndYear { get; set; }
    [Required, StringLength(200)] public string Level { get; set; } = "";
    [Required, StringLength(300)] public string Responsibility { get; set; } = "";
}

public sealed class PublicationEntry
{
    [Required, StringLength(1000)] public string Title { get; set; } = "";
    [Range(1900, 2100)] public int? Year { get; set; }
    [Required, StringLength(500)] public string Journal { get; set; } = "";
}
