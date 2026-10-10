using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Application.ProductTypes.Abstractions;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Products;

/// <summary>
/// Danh sách, chi tiết, thêm, sửa sản phẩm khoa học và đổi trạng thái bài báo.
/// Gửi duyệt, xét duyệt, minh chứng và lưu phiên bản thuộc các hạng mục sau.
/// </summary>
public sealed class ProductService(
    IProductRepository products,
    IProductTypeRepository productTypes,
    ILecturerRepository lecturers,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public Task<PagedResult<ProductSummaryResponse>> SearchAsync(
        ProductSearchQuery query, CancellationToken cancellationToken) =>
        products.SearchAsync(query, ProductAccess.VisibilityOf(currentUser), cancellationToken);

    public async Task<ProductDetailResponse> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var product = await products.GetViewAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        if (!ProductAccess.CanRead(currentUser, product.CreatedByAccountId, product.ReviewStatus, product.Authors))
            throw new ForbiddenException("Không có quyền xem sản phẩm này.");

        return WithPermissions(product);
    }

    public async Task<ProductDetailResponse> CreateAsync(
        CreateProductRequest request, CancellationToken cancellationToken)
    {
        var accountId = currentUser.Id ?? throw new ForbiddenException("Chưa đăng nhập.");

        var type = await productTypes.GetByIdAsync(request.ProductTypeId, cancellationToken)
            ?? throw new BusinessRuleException("Loại sản phẩm không tồn tại.");
        if (!await products.AcademicYearExistsAsync(request.AcademicYearId, cancellationToken))
            throw new BusinessRuleException("Năm học ghi nhận không tồn tại.");

        // Trạng thái bài báo chỉ có ở bài báo; bài báo mới mặc định là Đang viết.
        var articleStatus = TextNormalizer.Clean(request.ArticleStatus)?.ToUpperInvariant();
        if (type.Code == ProductTypeCodes.Article)
            articleStatus ??= ArticleStatuses.Writing;
        else if (articleStatus is not null)
            throw new BusinessRuleException("Trạng thái bài báo chỉ áp dụng cho sản phẩm loại Bài báo.");

        var content = NormalizeContent(request, articleStatus);
        await EnsureDoiAvailableAsync(content.Doi, null, cancellationToken);

        var mainLecturerId = await ResolveMainAuthorAsync(accountId, request.MainAuthorLecturerId, cancellationToken);
        var authorInputs = new List<ProductAuthorInput> { new() { LecturerId = mainLecturerId, Role = AuthorRoles.Main } };
        foreach (var co in request.CoAuthors ?? [])
        {
            if (co.Role.Trim().ToUpperInvariant() == AuthorRoles.Main)
                throw new BusinessRuleException("Chỉ có một tác giả chính; đồng tác giả phải có vai trò khác.");
            authorInputs.Add(co);
        }
        var authors = await ProductWriteGuard.PlanAuthorsAsync(lecturers, authorInputs, cancellationToken);

        var code = await NewCodeAsync(type.Code, cancellationToken);
        var product = new Product(type.Id, request.AcademicYearId, accountId, code, content, articleStatus);
        await products.CreateAsync(product, authors, cancellationToken);

        return await GetAfterSaveAsync(product.Id, cancellationToken);
    }

    public async Task<ProductDetailResponse> UpdateAsync(
        long id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var context = await ProductWriteGuard.LoadForWriteAsync(
            products, productTypes, currentUser, id, cancellationToken);
        var product = context.Product;

        if (request.AcademicYearId != product.AcademicYearId &&
            !await products.AcademicYearExistsAsync(request.AcademicYearId, cancellationToken))
            throw new BusinessRuleException("Năm học ghi nhận không tồn tại.");

        var content = NormalizeContent(request, product.ArticleStatus);
        await EnsureDoiAvailableAsync(content.Doi, id, cancellationToken);

        product.Update(request.AcademicYearId, content);
        await products.SaveChangesAsync(cancellationToken);

        return await GetAfterSaveAsync(id, cancellationToken);
    }

    /// <summary>
    /// Đổi tiến độ bài báo theo các bước hợp lệ (ArticleStatuses). Chuyển sang Đã xuất bản yêu cầu
    /// bài báo đã có tên tạp chí/hội nghị và năm công bố, vì quy đổi dựa vào hai trường này.
    /// </summary>
    public async Task<ProductDetailResponse> ChangeArticleStatusAsync(
        long id, ChangeArticleStatusRequest request, CancellationToken cancellationToken)
    {
        var context = await ProductWriteGuard.LoadForWriteAsync(
            products, productTypes, currentUser, id, cancellationToken);
        var product = context.Product;

        if (context.TypeCode != ProductTypeCodes.Article)
            throw new BusinessRuleException("Trạng thái bài báo chỉ áp dụng cho sản phẩm loại Bài báo.");

        var next = request.Status.Trim().ToUpperInvariant();
        if (!ArticleStatuses.All.Contains(next))
            throw new BusinessRuleException("Trạng thái bài báo không hợp lệ.");
        if (next == product.ArticleStatus)
            throw new BusinessRuleException("Bài báo đã ở trạng thái này.");
        if (!ArticleStatuses.CanTransition(product.ArticleStatus, next))
            throw new BusinessRuleException(
                $"Không thể chuyển bài báo từ '{product.ArticleStatus}' sang '{next}'.");
        EnsurePublishable(next, product.JournalName, product.PublicationYear);

        product.ChangeArticleStatus(next);
        await products.SaveChangesAsync(cancellationToken);

        return await GetAfterSaveAsync(id, cancellationToken);
    }

    // ──── Helpers ─────────────────────────────────────────────────────────────

    private ProductDetailResponse WithPermissions(ProductDetailResponse product) =>
        product with { Permissions = ProductAccess.PermissionsOf(currentUser, product) };

    private async Task<ProductDetailResponse> GetAfterSaveAsync(long id, CancellationToken cancellationToken) =>
        WithPermissions(await products.GetViewAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm."));

    /// <summary>
    /// Người dùng thường luôn là tác giả chính của sản phẩm mình tạo. Phòng QLKH/Quản trị được nhập hộ
    /// cho giảng viên khác, nên được chỉ định tác giả chính.
    /// </summary>
    private async Task<long> ResolveMainAuthorAsync(
        long accountId, long? requestedLecturerId, CancellationToken cancellationToken)
    {
        var own = await lecturers.GetViewByAccountIdAsync(accountId, cancellationToken);

        if (requestedLecturerId is { } requested && requested != own?.Id)
        {
            if (!ProductAccess.IsOffice(currentUser))
                throw new ForbiddenException("Chỉ Phòng QLKH hoặc quản trị viên được tạo sản phẩm thay giảng viên khác.");
            return requested;
        }

        return own?.Id ?? throw new BusinessRuleException(
            "Tài khoản chưa được liên kết với hồ sơ giảng viên. Hãy chọn tác giả chính hoặc liên hệ Phòng QLKH.");
    }

    private ProductContent NormalizeContent(ProductContentRequest r, string? articleStatus)
    {
        var title = r.Title.Trim();
        if (title.Length < 3)
            throw new BusinessRuleException("Tên sản phẩm phải có ít nhất 3 ký tự.");
        if (r.StartDate is { } start && r.EndDate is { } end && end < start)
            throw new BusinessRuleException("Ngày kết thúc không được trước ngày bắt đầu.");

        // Năm công bố lấy từ ngày xuất bản nếu người dùng chỉ nhập ngày.
        short? year = r.PublicationYear is { } y ? (short)y : r.PublishedDate is { } d ? (short)d.Year : null;
        if (year is < 1900 or > 2200)
            throw new BusinessRuleException("Năm công bố phải từ 1900 đến 2200.");

        var journalName = TextNormalizer.Clean(r.JournalName);
        EnsurePublishable(articleStatus, journalName, year);

        return new ProductContent(
            title, year, TextNormalizer.Clean(r.PublicationInfo), TextNormalizer.Clean(r.Doi),
            TextNormalizer.Clean(r.Isbn), TextNormalizer.Clean(r.Issn), journalName,
            TextNormalizer.Clean(r.JournalIndex), TextNormalizer.Clean(r.JournalCategory), r.WorkScore,
            TextNormalizer.Clean(r.ResearchField), TextNormalizer.Clean(r.Publisher),
            TextNormalizer.Clean(r.ProjectLevel), TextNormalizer.Clean(r.HostUnit),
            TextNormalizer.Clean(r.ProjectObjective), TextNormalizer.Clean(r.ProjectContent),
            TextNormalizer.Clean(r.ExpectedResult), TextNormalizer.Clean(r.CertificateNumber),
            TextNormalizer.Clean(r.IssuingAuthority), r.StartDate, r.EndDate, r.SubmittedDate, r.PublishedDate);
    }

    private static void EnsurePublishable(string? articleStatus, string? journalName, int? year)
    {
        if (articleStatus == ArticleStatuses.Published && (journalName is null || year is null))
            throw new BusinessRuleException(
                "Bài báo đã xuất bản cần có tên tạp chí/hội nghị và năm công bố. Hãy bổ sung rồi lưu lại.");
    }

    private async Task EnsureDoiAvailableAsync(string? doi, long? excludeProductId, CancellationToken cancellationToken)
    {
        if (doi is not null && await products.DoiExistsAsync(doi, excludeProductId, cancellationToken))
            throw new ConflictException("DOI này đã được dùng cho một sản phẩm khác.");
    }

    /// <summary>Mã dạng BB-2026-K7M2QX; ràng buộc UNIQUE của DB chặn trường hợp trùng hiếm gặp do đua nhau.</summary>
    private async Task<string> NewCodeAsync(string typeCode, CancellationToken cancellationToken)
    {
        var prefix = typeCode switch
        {
            ProductTypeCodes.Article => "BB",
            ProductTypeCodes.Project => "DT",
            ProductTypeCodes.Book => "SA",
            ProductTypeCodes.Certificate => "CN",
            _ => "SP",
        };
        var year = clock.GetUtcNow().Year;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = string.Create(6, 0, static (span, _) =>
            {
                for (var i = 0; i < span.Length; i++)
                    span[i] = CodeAlphabet[Random.Shared.Next(CodeAlphabet.Length)];
            });
            var code = $"{prefix}-{year}-{suffix}";
            if (!await products.CodeExistsAsync(code, cancellationToken)) return code;
        }
        throw new ConflictException("Không tạo được mã sản phẩm. Vui lòng thử lại.");
    }
}
