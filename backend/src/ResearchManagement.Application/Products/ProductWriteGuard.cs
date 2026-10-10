using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Application.ProductTypes.Abstractions;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Products;

internal sealed record ProductWriteContext(
    Product Product, IReadOnlyList<ProductAuthorResponse> Authors, string TypeCode);

/// <summary>Kiểm tra dùng chung cho mọi thao tác ghi lên một sản phẩm đã có.</summary>
internal static class ProductWriteGuard
{
    public const int MaxAuthors = 30;

    /// <summary>Tồn tại, được xem, được quản lý và đang ở trạng thái xét duyệt cho phép sửa.</summary>
    public static async Task<ProductWriteContext> LoadForWriteAsync(
        IProductRepository products, IProductTypeRepository productTypes, ICurrentUser user,
        long id, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        var authors = await products.GetAuthorsAsync(id, cancellationToken);

        if (!ProductAccess.CanRead(user, product.CreatedByAccountId, product.ReviewStatus, authors))
            throw new ForbiddenException("Không có quyền xem sản phẩm này.");
        if (!ProductAccess.CanManage(user, product.CreatedByAccountId, authors))
            throw new ForbiddenException("Chỉ người tạo hoặc tác giả chính được chỉnh sửa sản phẩm.");
        if (!ReviewStatuses.IsEditable(product.ReviewStatus))
            throw new BusinessRuleException(
                "Sản phẩm đã gửi duyệt hoặc đã chốt nên không thể chỉnh sửa. Chỉ sửa được khi ở trạng thái Nháp hoặc Cần bổ sung.");

        var type = await productTypes.GetByIdAsync(product.ProductTypeId, cancellationToken)
            ?? throw new BusinessRuleException("Loại sản phẩm không còn tồn tại.");
        return new ProductWriteContext(product, authors, type.Code);
    }

    /// <summary>
    /// Kiểm tra danh sách tác giả mong muốn rồi đánh lại thứ tự 1..n theo thứ tự trong danh sách.
    /// Bộ môn ghi nhận lấy từ hồ sơ giảng viên hiện tại (repository chỉ dùng cho người mới thêm).
    /// </summary>
    public static async Task<IReadOnlyList<AuthorSlot>> PlanAuthorsAsync(
        ILecturerRepository lecturers, IReadOnlyList<ProductAuthorInput> inputs, CancellationToken cancellationToken)
    {
        if (inputs.Count == 0)
            throw new BusinessRuleException("Sản phẩm phải có ít nhất một tác giả.");
        if (inputs.Count > MaxAuthors)
            throw new BusinessRuleException($"Một sản phẩm có tối đa {MaxAuthors} tác giả.");

        var roles = inputs.Select(i => i.Role.Trim().ToUpperInvariant()).ToList();
        if (roles.Any(r => !AuthorRoles.All.Contains(r)))
            throw new BusinessRuleException("Vai trò tác giả không hợp lệ.");
        if (roles.Count(r => r == AuthorRoles.Main) != 1)
            throw new BusinessRuleException("Sản phẩm phải có đúng một tác giả chính.");
        if (inputs.Select(i => i.LecturerId).Distinct().Count() != inputs.Count)
            throw new BusinessRuleException("Một giảng viên chỉ được xuất hiện một lần trong danh sách tác giả.");

        var slots = new List<AuthorSlot>(inputs.Count);
        for (var i = 0; i < inputs.Count; i++)
        {
            var lecturer = await lecturers.GetViewAsync(inputs[i].LecturerId, cancellationToken)
                ?? throw new BusinessRuleException($"Giảng viên có mã hệ thống {inputs[i].LecturerId} không tồn tại.");
            slots.Add(new AuthorSlot(lecturer.Id, lecturer.DepartmentId, roles[i], i + 1));
        }
        return slots;
    }
}
