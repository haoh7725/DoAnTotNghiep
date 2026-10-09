
using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng giang_vien. Mỗi tài khoản liên kết tối đa một giảng viên.</summary>
public sealed class Lecturer : BaseEntity
{
    private Lecturer() { }

    public Lecturer(
        long departmentId, long accountId, string code, string fullName,
        DateOnly? birthDate, string? gender, string? email, string? phone,
        string? academicRank, string? degree, string? position)
    {
        AccountId = accountId;
        Update(departmentId, code, fullName, birthDate, gender, email, phone, academicRank, degree, position);
    }

    public long DepartmentId { get; private set; }
    public long AccountId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public DateOnly? BirthDate { get; private set; }
    public string? Gender { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? AcademicRank { get; private set; }
    public string? Degree { get; private set; }
    public string? Position { get; private set; }

    /// <summary>Cập nhật toàn bộ thông tin, do Phòng QLKH/Quản trị thực hiện.</summary>
    public void Update(
        long departmentId, string code, string fullName,
        DateOnly? birthDate, string? gender, string? email, string? phone,
        string? academicRank, string? degree, string? position)
    {
        DepartmentId = departmentId;
        Code = code;
        AcademicRank = academicRank;
        Degree = degree;
        Position = position;
        UpdatePersonal(fullName, birthDate, gender, email, phone);
    }

    /// <summary>Thông tin cá nhân mà chính giảng viên được tự sửa.</summary>
    public void UpdatePersonal(
        string fullName, DateOnly? birthDate, string? gender,
        string? email, string? phone)
    {
        FullName = fullName;
        BirthDate = birthDate;
        Gender = gender;
        Email = email;
        Phone = phone;
    }
}
