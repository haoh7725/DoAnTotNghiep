namespace ResearchManagement.Domain.Constants;

/// <summary>Các trạng thái của sản phẩm NCKH.</summary>
public static class ProductStatuses
{
    /// <summary>Bản nháp, chưa nộp.</summary>
    public const string Draft = "NHAP";

    /// <summary>Đã nộp, chờ xét duyệt cấp Bộ môn.</summary>
    public const string Submitted = "NOP";

    /// <summary>Trưởng bộ môn đã duyệt.</summary>
    public const string DepartmentHeadApproved = "TRUONG_BO_MON_DUYET";

    /// <summary>Trưởng khoa đã duyệt.</summary>
    public const string FacultyDeanApproved = "TRUONG_KHOA_DUYET";

    /// <summary>Phòng QLKH đã duyệt (hoàn tất).</summary>
    public const string ResearchOfficeApproved = "PHONG_QLKH_DUYET";

    /// <summary>Đã duyệt hoàn toàn, tính điểm quy đổi.</summary>
    public const string Approved = "DA_DUYET";

    /// <summary>Trả lại để giảng viên chỉnh sửa.</summary>
    public const string Returned = "TRA_LAI";

    public static readonly string[] All =
    [
        Draft, Submitted,
        DepartmentHeadApproved, FacultyDeanApproved, ResearchOfficeApproved,
        Approved, Returned
    ];
}
