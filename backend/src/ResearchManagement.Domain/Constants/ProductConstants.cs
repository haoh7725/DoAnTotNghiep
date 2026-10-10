namespace ResearchManagement.Domain.Constants;

/// <summary>Giá trị cột san_pham_khoa_hoc.trang_thai_duyet (trạng thái xét duyệt, khác trạng thái bài báo).</summary>
public static class ReviewStatuses
{
    public const string Draft = "NHAP";
    public const string Pending = "CHO_DUYET";
    public const string NeedsRevision = "CAN_BO_SUNG";
    public const string Approved = "DA_DUYET";
    public const string Rejected = "TU_CHOI";

    public static readonly string[] All = [Draft, Pending, NeedsRevision, Approved, Rejected];

    /// <summary>Chỉ khi chưa gửi duyệt hoặc đang được yêu cầu bổ sung thì tác giả mới được sửa.</summary>
    public static bool IsEditable(string status) => status is Draft or NeedsRevision;
}

/// <summary>
/// Giá trị cột san_pham_khoa_hoc.trang_thai_bai_bao: tiến độ của bài báo, chỉ áp dụng cho loại BAI_BAO.
/// </summary>
public static class ArticleStatuses
{
    public const string Writing = "DANG_VIET";
    public const string UnderReview = "DANG_PHAN_BIEN";
    public const string Reviewed = "DA_NHAN_XET";
    public const string Published = "DA_XUAT_BAN";

    public static readonly string[] All = [Writing, UnderReview, Reviewed, Published];

    private static readonly Dictionary<string, string[]> Transitions = new()
    {
        [Writing] = [UnderReview],
        // Rút bài về viết lại hoặc nhận xét xong.
        [UnderReview] = [Reviewed, Writing],
        // Sửa theo nhận xét rồi nộp lại, hoặc được nhận đăng, hoặc quay về viết lại.
        [Reviewed] = [UnderReview, Published, Writing],
        // Đã xuất bản là trạng thái cuối.
        [Published] = [],
    };

    /// <summary>Các trạng thái được phép chuyển tới từ trạng thái hiện tại (null = chưa có, chọn tự do).</summary>
    public static IReadOnlyList<string> NextFrom(string? current) =>
        current is null ? All : Transitions.GetValueOrDefault(current) ?? [];

    public static bool CanTransition(string? from, string to) => NextFrom(from).Contains(to);
}

/// <summary>Mã loại sản phẩm (cột loai_san_pham.ma_loai) mà biểu mẫu và quy tắc nghiệp vụ dựa vào.</summary>
public static class ProductTypeCodes
{
    public const string Article = "BAI_BAO";
    public const string Project = "DE_TAI";
    public const string Book = "SACH";
    public const string Certificate = "CHUNG_NHAN";
}

/// <summary>
/// Giá trị cột tham_gia_san_pham.vai_tro_tac_gia. Cũng là khóa của bảng hệ số tác giả khi quy đổi
/// (he_so_tac_gia_quy_doi.vai_tro_tac_gia), nên quy định quy đổi phải dùng đúng các mã này.
/// </summary>
public static class AuthorRoles
{
    public const string Main = "TAC_GIA_CHINH";
    public const string Corresponding = "TAC_GIA_LIEN_HE";
    public const string CoAuthor = "DONG_TAC_GIA";

    public static readonly string[] All = [Main, Corresponding, CoAuthor];
}
