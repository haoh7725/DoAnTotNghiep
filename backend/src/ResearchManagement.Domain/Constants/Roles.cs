namespace ResearchManagement.Domain.Constants;

/// <summary>Giá trị cột phan_quyen.vai_tro.</summary>
public static class Roles
{
    public const string Lecturer = "GIANG_VIEN";
    public const string DepartmentHead = "TRUONG_BO_MON";
    public const string FacultyDean = "TRUONG_KHOA";
    public const string ResearchOffice = "PHONG_QLKH";
    public const string BoardOfDirectors = "BAN_GIAM_HIEU";
    public const string Admin = "QUAN_TRI";

    public static readonly string[] All =
        [Lecturer, DepartmentHead, FacultyDean, ResearchOffice, BoardOfDirectors, Admin];
}

/// <summary>Giá trị cột phan_quyen.pham_vi.</summary>
public static class Scopes
{
    public const string Personal = "CA_NHAN";
    public const string Department = "BO_MON";
    public const string Faculty = "KHOA";
    public const string Global = "TOAN_TRUONG";
}

/// <summary>Giá trị cột tai_khoan.trang_thai.</summary>
public static class AccountStatuses
{
    public const string Active = "HOAT_DONG";
    public const string Locked = "KHOA";
}
