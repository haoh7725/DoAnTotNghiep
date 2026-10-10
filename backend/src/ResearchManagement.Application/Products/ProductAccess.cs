using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Application.Products;

/// <summary>
/// Quy tắc truy cập sản phẩm khoa học. Backend là nơi quyết định cuối cùng; giao diện chỉ dùng
/// <see cref="ProductPermissionsResponse"/> để ẩn/hiện nút.
/// </summary>
public static class ProductAccess
{
    public static bool IsAdmin(ICurrentUser user) => user.IsInRole(Roles.Admin);

    public static bool IsOffice(ICurrentUser user) => user.IsInRole(Roles.ResearchOffice) || IsAdmin(user);

    /// <summary>Người tạo hoặc một trong các tác giả của sản phẩm.</summary>
    public static bool IsParticipant(
        ICurrentUser user, long createdByAccountId, IReadOnlyList<ProductAuthorResponse> authors) =>
        user.Id is { } id && (createdByAccountId == id || authors.Any(a => a.AccountId == id));

    /// <summary>
    /// Xem: Quản trị, người tạo/tác giả, hoặc người có phạm vi KHOA/BO_MON/TOAN_TRUONG bao trùm đơn vị
    /// của một tác giả. Bản nháp chỉ người trong cuộc và Quản trị được xem.
    /// </summary>
    public static bool CanRead(
        ICurrentUser user, long createdByAccountId, string reviewStatus, IReadOnlyList<ProductAuthorResponse> authors) =>
        IsAdmin(user)
        || IsParticipant(user, createdByAccountId, authors)
        || (reviewStatus != ReviewStatuses.Draft && authors.Any(a => user.CanAccess(a.FacultyId, a.DepartmentId)));

    /// <summary>Sửa nội dung, đổi trạng thái bài báo, quản lý tác giả: người tạo, tác giả chính hoặc Quản trị.</summary>
    public static bool CanManage(
        ICurrentUser user, long createdByAccountId, IReadOnlyList<ProductAuthorResponse> authors) =>
        IsAdmin(user)
        || (user.Id is { } id
            && (createdByAccountId == id || authors.Any(a => a.Role == AuthorRoles.Main && a.AccountId == id)));

    public static ProductPermissionsResponse PermissionsOf(ICurrentUser user, ProductDetailResponse product)
    {
        var editable = CanManage(user, product.CreatedByAccountId, product.Authors)
                       && ReviewStatuses.IsEditable(product.ReviewStatus);
        var next = editable && product.ProductTypeCode == ProductTypeCodes.Article
            ? ArticleStatuses.NextFrom(product.ArticleStatus)
            : [];
        return new ProductPermissionsResponse(editable, editable, next.Count > 0, next);
    }

    public static ProductVisibility VisibilityOf(ICurrentUser user) => new(
        IsAdmin(user),
        user.Assignments.Any(a => a.Scope == Scopes.Global),
        user.Assignments.Where(a => a.Scope == Scopes.Faculty && a.FacultyId.HasValue)
            .Select(a => a.FacultyId!.Value).Distinct().ToArray(),
        user.Assignments.Where(a => a.Scope == Scopes.Department && a.DepartmentId.HasValue)
            .Select(a => a.DepartmentId!.Value).Distinct().ToArray(),
        user.Id);
}
