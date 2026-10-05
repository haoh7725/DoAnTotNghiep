using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>
/// Bảng nckh.dong_tac_gia.
/// Đồng tác giả của sản phẩm NCKH — chỉ giảng viên trong hệ thống.
/// </summary>
public sealed class ProductCoAuthor : BaseEntity
{
    private ProductCoAuthor() { }

    public ProductCoAuthor(
        long productId,
        long lecturerId,
        int displayOrder)
    {
        ProductId = productId;
        LecturerId = lecturerId;
        DisplayOrder = displayOrder;
    }

    /// <summary>FK → san_pham.id</summary>
    public long ProductId { get; private set; }

    /// <summary>FK → giang_vien.id</summary>
    public long LecturerId { get; private set; }

    /// <summary>Thứ tự hiển thị (1 = tác giả chính thứ nhất, tăng dần).</summary>
    public int DisplayOrder { get; private set; }

    public void UpdateOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }
}
