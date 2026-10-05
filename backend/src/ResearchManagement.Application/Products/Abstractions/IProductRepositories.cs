using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Products.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>Lấy kèm đồng tác giả và minh chứng.</summary>
    Task<Product?> GetByIdWithDetailsAsync(long id, CancellationToken cancellationToken);

    Task<List<Product>> GetByPlanItemIdAsync(long planItemId, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IProductCoAuthorRepository
{
    Task<ProductCoAuthor?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<List<ProductCoAuthor>> GetByProductIdAsync(long productId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(long productId, long lecturerId, CancellationToken cancellationToken);

    Task AddAsync(ProductCoAuthor coAuthor, CancellationToken cancellationToken);

    void Remove(ProductCoAuthor coAuthor);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IProductEvidenceRepository
{
    Task<ProductEvidence?> GetByIdAsync(long id, CancellationToken cancellationToken);

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
