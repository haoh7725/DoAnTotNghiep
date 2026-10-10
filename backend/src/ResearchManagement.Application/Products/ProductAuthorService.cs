using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Application.Lecturers.Models;
using ResearchManagement.Application.ProductTypes.Abstractions;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Application.Products;

/// <summary>Quản lý tác giả và đồng tác giả (bảng tham_gia_san_pham).</summary>
public sealed class ProductAuthorService(
    IProductRepository products,
    IProductTypeRepository productTypes,
    ILecturerRepository lecturers,
    ICurrentUser currentUser)
{
    /// <summary>
    /// Tìm giảng viên toàn trường để chọn làm đồng tác giả. Khác danh sách hồ sơ giảng viên (lọc theo phạm vi),
    /// đồng tác giả có thể ở bất kỳ bộ môn nào nên chỉ trả thông tin tối thiểu.
    /// </summary>
    public async Task<IReadOnlyList<AuthorCandidateResponse>> SearchCandidatesAsync(
        string? keyword, CancellationToken cancellationToken)
    {
        var term = keyword?.Trim();
        if (string.IsNullOrEmpty(term) || term.Length < 2)
            throw new BusinessRuleException("Nhập ít nhất 2 ký tự để tìm giảng viên.");

        var everyone = new LecturerVisibility(true, [], [], null);
        var page = await lecturers.SearchAsync(
            new LecturerSearchQuery { Keyword = term.Length > 100 ? term[..100] : term, PageSize = 10 },
            everyone, cancellationToken);
        return page.Items
            .Select(l => new AuthorCandidateResponse(l.Id, l.Code, l.FullName, l.DepartmentName, l.FacultyName))
            .ToList();
    }

    public async Task<IReadOnlyList<ProductAuthorResponse>> GetAsync(long productId, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        var authors = await products.GetAuthorsAsync(productId, cancellationToken);

        if (!ProductAccess.CanRead(currentUser, product.CreatedByAccountId, product.ReviewStatus, authors))
            throw new ForbiddenException("Không có quyền xem sản phẩm này.");
        return authors;
    }

    /// <summary>Thay toàn bộ danh sách: thêm, bớt, đổi vai trò và thứ tự trong một lần lưu.</summary>
    public async Task<IReadOnlyList<ProductAuthorResponse>> ReplaceAsync(
        long productId, ReplaceProductAuthorsRequest request, CancellationToken cancellationToken)
    {
        await ProductWriteGuard.LoadForWriteAsync(products, productTypes, currentUser, productId, cancellationToken);

        var slots = await ProductWriteGuard.PlanAuthorsAsync(lecturers, request.Authors, cancellationToken);
        await products.ReplaceAuthorsAsync(productId, slots, cancellationToken);
        return await products.GetAuthorsAsync(productId, cancellationToken);
    }

    /// <summary>Thêm một đồng tác giả vào cuối danh sách. Muốn đổi tác giả chính thì dùng <see cref="ReplaceAsync"/>.</summary>
    public async Task<IReadOnlyList<ProductAuthorResponse>> AddAsync(
        long productId, AddProductAuthorRequest request, CancellationToken cancellationToken)
    {
        var context = await ProductWriteGuard.LoadForWriteAsync(
            products, productTypes, currentUser, productId, cancellationToken);

        if (request.Role.Trim().ToUpperInvariant() == AuthorRoles.Main)
            throw new BusinessRuleException(
                "Sản phẩm đã có tác giả chính. Muốn đổi tác giả chính hãy cập nhật cả danh sách tác giả.");
        if (context.Authors.Any(a => a.LecturerId == request.LecturerId))
            throw new ConflictException("Giảng viên này đã là tác giả của sản phẩm.");

        var inputs = context.Authors
            .OrderBy(a => a.Order)
            .Select(a => new ProductAuthorInput { LecturerId = a.LecturerId, Role = a.Role })
            .Append(new ProductAuthorInput { LecturerId = request.LecturerId, Role = request.Role })
            .ToList();

        var slots = await ProductWriteGuard.PlanAuthorsAsync(lecturers, inputs, cancellationToken);
        await products.ReplaceAuthorsAsync(productId, slots, cancellationToken);
        return await products.GetAuthorsAsync(productId, cancellationToken);
    }

    /// <summary>Bỏ một đồng tác giả; thứ tự những người còn lại được dồn lại liên tục từ 1.</summary>
    public async Task<IReadOnlyList<ProductAuthorResponse>> RemoveAsync(
        long productId, long lecturerId, CancellationToken cancellationToken)
    {
        var context = await ProductWriteGuard.LoadForWriteAsync(
            products, productTypes, currentUser, productId, cancellationToken);

        var target = context.Authors.FirstOrDefault(a => a.LecturerId == lecturerId)
            ?? throw new NotFoundException("Giảng viên này không phải tác giả của sản phẩm.");
        if (target.Role == AuthorRoles.Main)
            throw new BusinessRuleException(
                "Không thể bỏ tác giả chính. Hãy chuyển vai trò tác giả chính cho người khác trước.");

        var inputs = context.Authors
            .Where(a => a.LecturerId != lecturerId)
            .OrderBy(a => a.Order)
            .Select(a => new ProductAuthorInput { LecturerId = a.LecturerId, Role = a.Role })
            .ToList();

        var slots = await ProductWriteGuard.PlanAuthorsAsync(lecturers, inputs, cancellationToken);
        await products.ReplaceAuthorsAsync(productId, slots, cancellationToken);
        return await products.GetAuthorsAsync(productId, cancellationToken);
    }
}
