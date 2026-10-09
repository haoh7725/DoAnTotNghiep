using System.ComponentModel.DataAnnotations;
using ResearchManagement.Application.Common;

namespace ResearchManagement.Application.Lecturers.Models;

public sealed class LecturerSearchQuery : PagedQuery
{
    /// <summary>Tìm theo họ tên hoặc mã giảng viên, không phân biệt hoa thường.</summary>
    [StringLength(100)]
    public string? Keyword { get; set; }

    [Range(1, long.MaxValue)]
    public long? FacultyId { get; set; }

    [Range(1, long.MaxValue)]
    public long? DepartmentId { get; set; }
}

/// <summary>Thông tin cá nhân mà giảng viên được tự sửa qua PUT /api/lecturers/me.</summary>
public class UpdateMyLecturerRequest
{
    [Required, StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    /// <summary>NAM, NU hoặc KHAC. Giá trị rỗng gửi là null.</summary>
    [RegularExpression("^(NAM|NU|KHAC)$", ErrorMessage = "Giới tính phải là NAM, NU hoặc KHAC.")]
    public string? Gender { get; set; }

    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }

    [StringLength(30), RegularExpression(@"^\+?[0-9 .()\-]{8,30}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string? Phone { get; set; }
}

/// <summary>Toàn bộ thông tin giảng viên, do Phòng QLKH/Quản trị sửa qua PUT /api/lecturers/{id}.</summary>
public class UpdateLecturerRequest : UpdateMyLecturerRequest
{
    [Range(1, long.MaxValue)]
    public long DepartmentId { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [StringLength(100)]
    public string? AcademicRank { get; set; }

    [StringLength(100)]
    public string? Degree { get; set; }

    [StringLength(100)]
    public string? Position { get; set; }
}

public sealed class CreateLecturerRequest : UpdateLecturerRequest
{
    [Range(1, long.MaxValue)]
    public long AccountId { get; set; }
}

public sealed record LecturerResponse(
    long Id,
    long AccountId,
    string Code,
    string FullName,
    DateOnly? BirthDate,
    string? Gender,
    string? Email,
    string? Phone,
    string? AcademicRank,
    string? Degree,
    string? Position,
    long DepartmentId,
    string DepartmentName,
    long FacultyId,
    string FacultyName);

/// <summary>Phạm vi giảng viên mà người dùng hiện tại được liệt kê, suy ra từ phân quyền.</summary>
public sealed record LecturerVisibility(
    bool All,
    IReadOnlyCollection<long> FacultyIds,
    IReadOnlyCollection<long> DepartmentIds,
    long? OwnAccountId);

public sealed class ScientificProfileRequest
{
    [StringLength(4000)] public string? Expertise { get; set; }
    [StringLength(4000)] public string? ResearchFields { get; set; }
    [StringLength(4000)] public string? ResearchDirections { get; set; }
    [StringLength(4000)] public string? ActivitySummary { get; set; }
}

/// <summary>UpdatedAt = null nghĩa là giảng viên chưa lập lý lịch; các trường văn bản đều null.</summary>
public sealed record ScientificProfileResponse(
    long LecturerId,
    string? Expertise,
    string? ResearchFields,
    string? ResearchDirections,
    string? ActivitySummary,
    DateTimeOffset? UpdatedAt);
