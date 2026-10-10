namespace ResearchManagement.Domain.Entities;

/// <summary>
/// Bảng tham_gia_san_pham: một giảng viên tham gia một sản phẩm (khóa chính kép).
/// Bộ môn được ghi nhận tại thời điểm thêm để thống kê không đổi khi giảng viên chuyển bộ môn.
/// </summary>
public sealed class ProductAuthor
{
    private ProductAuthor() { }

    public ProductAuthor(long productId, long lecturerId, long departmentId, string role, int order)
    {
        ProductId = productId;
        LecturerId = lecturerId;
        DepartmentId = departmentId;
        Role = role;
        Order = order;
    }

    public long ProductId { get; private set; }
    public long LecturerId { get; private set; }

    /// <summary>bo_mon_id_ghi_nhan: bộ môn của giảng viên khi được thêm vào sản phẩm.</summary>
    public long DepartmentId { get; private set; }

    /// <summary>Một trong AuthorRoles.</summary>
    public string Role { get; private set; } = string.Empty;

    /// <summary>Thứ tự tác giả, bắt đầu từ 1 và không trùng trong cùng sản phẩm.</summary>
    public int Order { get; private set; }

    public void Change(string role, int order)
    {
        Role = role;
        Order = order;
    }
}
