using ResearchManagement.Application.Common;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Products;

public sealed class CoAuthorService(
    IProductRepository products,
    IProductCoAuthorRepository coAuthors,
    ILecturerRepository lecturers,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<CoAuthorResponse>> GetByProductIdAsync(
        long productId,
        CancellationToken cancellationToken)
    {
        _ = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        var list = await coAuthors.GetByProductIdAsync(productId, cancellationToken);
        return await EnrichAsync(list, cancellationToken);
    }

    public async Task<CoAuthorResponse> AddAsync(
        long productId,
        AddCoAuthorRequest request,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        EnsureEditable(product);

        var lecturer = await lecturers.GetByIdAsync(request.LecturerId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");

        if (lecturer.Id == product.SubmittedByLecturerId)
            throw new BusinessRuleException("Tác giả chính đã có trong sản phẩm, không cần thêm vào đồng tác giả.");

        if (await coAuthors.ExistsAsync(productId, request.LecturerId, cancellationToken))
            throw new ConflictException("Giảng viên này đã là đồng tác giả của sản phẩm.");

        var coAuthor = new ProductCoAuthor(productId, request.LecturerId, request.DisplayOrder);
        await coAuthors.AddAsync(coAuthor, cancellationToken);
        await coAuthors.SaveChangesAsync(cancellationToken);

        return new CoAuthorResponse(
            coAuthor.Id, coAuthor.ProductId, coAuthor.LecturerId,
            lecturer.FullName, lecturer.Code, coAuthor.DisplayOrder);
    }

    public async Task<CoAuthorResponse> UpdateAsync(
        long productId,
        long coAuthorId,
        UpdateCoAuthorRequest request,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        EnsureEditable(product);

        var coAuthor = await coAuthors.GetByIdAsync(coAuthorId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy đồng tác giả.");

        if (coAuthor.ProductId != productId)
            throw new NotFoundException("Đồng tác giả không thuộc sản phẩm này.");

        coAuthor.UpdateOrder(request.DisplayOrder);
        await coAuthors.SaveChangesAsync(cancellationToken);

        var lecturer = await lecturers.GetByIdAsync(coAuthor.LecturerId, cancellationToken);
        return new CoAuthorResponse(
            coAuthor.Id, coAuthor.ProductId, coAuthor.LecturerId,
            lecturer?.FullName ?? string.Empty, lecturer?.Code, coAuthor.DisplayOrder);
    }

    public async Task RemoveAsync(
        long productId,
        long coAuthorId,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        EnsureEditable(product);

        var coAuthor = await coAuthors.GetByIdAsync(coAuthorId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy đồng tác giả.");

        if (coAuthor.ProductId != productId)
            throw new NotFoundException("Đồng tác giả không thuộc sản phẩm này.");

        coAuthors.Remove(coAuthor);
        await coAuthors.SaveChangesAsync(cancellationToken);
    }

    // ──── Helpers ─────────────────────────────────────────────────────────────

    private static void EnsureEditable(Product product)
    {
        if (product.Status is not (ProductStatuses.Draft or ProductStatuses.Returned))
            throw new BusinessRuleException("Chỉ có thể chỉnh sửa đồng tác giả khi sản phẩm ở trạng thái Nháp hoặc Trả lại.");
    }

    private async Task<IReadOnlyList<CoAuthorResponse>> EnrichAsync(
        List<ProductCoAuthor> list,
        CancellationToken cancellationToken)
    {
        var result = new List<CoAuthorResponse>();
        foreach (var ca in list.OrderBy(c => c.DisplayOrder))
        {
            var lec = await lecturers.GetByIdAsync(ca.LecturerId, cancellationToken);
            result.Add(new CoAuthorResponse(
                ca.Id, ca.ProductId, ca.LecturerId,
                lec?.FullName ?? string.Empty, lec?.Code, ca.DisplayOrder));
        }
        return result;
    }
}
