using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Lecturers.Models;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Application.Lecturers;

/// <summary>Quy tắc truy cập hồ sơ giảng viên, dùng chung cho hồ sơ và lý lịch khoa học.</summary>
public static class LecturerAccess
{
    /// <summary>Phòng QLKH hoặc Quản trị: được tạo, sửa mọi hồ sơ và lý lịch.</summary>
    public static bool IsOffice(ICurrentUser user) =>
        user.IsInRole(Roles.ResearchOffice) || user.IsInRole(Roles.Admin);

    public static bool IsOwner(ICurrentUser user, LecturerResponse lecturer) =>
        user.Id is { } id && id == lecturer.AccountId;

    /// <summary>Xem: chính chủ, hoặc có phạm vi KHOA/BO_MON/TOAN_TRUONG bao trùm đơn vị của giảng viên.</summary>
    public static bool CanRead(ICurrentUser user, LecturerResponse lecturer) =>
        IsOwner(user, lecturer) || user.CanAccess(lecturer.FacultyId, lecturer.DepartmentId);

    /// <summary>Sửa lý lịch khoa học: chính chủ hoặc Phòng QLKH/Quản trị. Trưởng khoa, trưởng bộ môn chỉ xem.</summary>
    public static bool CanEditProfile(ICurrentUser user, LecturerResponse lecturer) =>
        IsOwner(user, lecturer) || IsOffice(user);

    public static LecturerVisibility VisibilityOf(ICurrentUser user) => new(
        user.Assignments.Any(a => a.Scope == Scopes.Global),
        user.Assignments.Where(a => a.Scope == Scopes.Faculty && a.FacultyId.HasValue)
            .Select(a => a.FacultyId!.Value).Distinct().ToArray(),
        user.Assignments.Where(a => a.Scope == Scopes.Department && a.DepartmentId.HasValue)
            .Select(a => a.DepartmentId!.Value).Distinct().ToArray(),
        user.Id);
}
