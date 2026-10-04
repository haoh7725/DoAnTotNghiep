using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ProductTypes.Abstractions;

public interface IProductTypeRepository
{
    Task<IReadOnlyList<ProductType>> ListAsync(CancellationToken cancellationToken);
    Task<ProductType?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(ProductType productType, CancellationToken cancellationToken);
    void Remove(ProductType productType);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
