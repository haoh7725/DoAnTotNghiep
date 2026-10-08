using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>
/// Bảng loai_san_pham. Mã loại (BAI_BAO, DE_TAI, SACH, CHUNG_NHAN, ...) là định danh ổn định
/// mà giao diện dùng để chọn biểu mẫu nên chỉ đặt khi tạo, không đổi sau đó.
/// </summary>
public sealed class ProductType : BaseEntity
{
    private ProductType() { }

    public ProductType(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    public void Rename(string name) => Name = name;
}
