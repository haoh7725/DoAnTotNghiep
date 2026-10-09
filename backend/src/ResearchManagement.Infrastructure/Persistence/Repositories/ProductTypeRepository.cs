using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.ProductTypes.Abstractions;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class ProductTypeRepository(ApplicationDbContext db) : IProductTypeRepository
{
    public async Task<IReadOnlyList<ProductType>> ListAsync(CancellationToken cancellationToken) =>
        await db.ProductTypes.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);

    public Task<ProductType?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        db.ProductTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken) =>
        db.ProductTypes.AnyAsync(x => x.Code == code, cancellationToken);

    public async Task AddAsync(ProductType productType, CancellationToken cancellationToken) =>
        await db.ProductTypes.AddAsync(productType, cancellationToken);

    public void Remove(ProductType productType) => db.ProductTypes.Remove(productType);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
