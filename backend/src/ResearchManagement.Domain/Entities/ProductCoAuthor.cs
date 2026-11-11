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
        int displayOrder,
        string authorRole = "DONG_TAC_GIA")
    {
        ProductId = productId;
        LecturerId = lecturerId;
        DisplayOrder = displayOrder;
        AuthorRole = NormalizeAuthorRole(authorRole);
    }

    /// <summary>FK → san_pham.id</summary>
    public long ProductId { get; private set; }

    /// <summary>FK → giang_vien.id</summary>
    public long LecturerId { get; private set; }

    /// <summary>Thứ tự hiển thị (1 = tác giả chính thứ nhất, tăng dần).</summary>
    public int DisplayOrder { get; private set; }
    public string AuthorRole { get; private set; } = "DONG_TAC_GIA";

    public void Update(int displayOrder, string authorRole)
    {
        DisplayOrder = displayOrder;
        AuthorRole = NormalizeAuthorRole(authorRole);
    }

    private static string NormalizeAuthorRole(string authorRole)
    {
        var normalized = authorRole.Trim().ToUpperInvariant();
        if (normalized is not ("DONG_TAC_GIA" or "TAC_GIA_LIEN_HE"))
            throw new ArgumentException("Vai trò đồng tác giả không hợp lệ.", nameof(authorRole));
        return normalized;
    }
}
