using ResearchManagement.Application.Common;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Products.Abstractions;

public interface IProductRepository
{
    // Đọc: trả về dữ liệu đã nối loại sản phẩm, năm học, người tạo và tác giả.
    Task<ProductDetailResponse?> GetViewAsync(long id, CancellationToken cancellationToken);

    Task<PagedResult<ProductSummaryResponse>> SearchAsync(
        ProductSearchQuery query, ProductVisibility visibility, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductAuthorResponse>> GetAuthorsAsync(long productId, CancellationToken cancellationToken);

    // Ghi: trả về entity được theo dõi.
    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<bool> AcademicYearExistsAsync(long academicYearId, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);

    /// <summary>DOI duy nhất không phân biệt hoa thường (chỉ mục uq_san_pham_doi).</summary>
    Task<bool> DoiExistsAsync(string doi, long? excludeProductId, CancellationToken cancellationToken);

    /// <summary>Lưu sản phẩm cùng danh sách tác giả ban đầu trong một giao dịch.</summary>
    Task CreateAsync(Product product, IReadOnlyList<AuthorSlot> authors, CancellationToken cancellationToken);

    /// <summary>
    /// Đồng bộ danh sách tác giả với danh sách mong muốn trong một giao dịch: xóa người bị bỏ,
    /// thêm người mới, đổi vai trò/thứ tự người còn lại (không đổi bộ môn đã ghi nhận).
    /// </summary>
    Task ReplaceAuthorsAsync(long productId, IReadOnlyList<AuthorSlot> authors, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IProductEvidenceRepository
{
    Task<ProductEvidence?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<EvidenceResponse?> GetViewByIdAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<EvidenceResponse>> GetViewsByProductIdAsync(long productId, CancellationToken cancellationToken);

    Task<List<ProductEvidence>> GetByProductIdAsync(long productId, CancellationToken cancellationToken);

    Task AddAsync(ProductEvidence evidence, CancellationToken cancellationToken);

    void Remove(ProductEvidence evidence);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IReviewHistoryRepository
{
    Task<List<ReviewHistory>> GetByProductIdAsync(long productId, CancellationToken cancellationToken);

    Task AddAsync(ReviewHistory history, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
