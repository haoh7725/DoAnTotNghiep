using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(ApplicationDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetByIdWithDetailsAsync(long id, CancellationToken cancellationToken) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<List<Product>> GetByPlanItemIdAsync(long planItemId, CancellationToken cancellationToken) =>
        db.Products
            .Where(p => p.ResearchPlanItemId == planItemId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<List<Product>> GetByLecturerIdAsync(long lecturerId, CancellationToken cancellationToken) =>
        db.Products.Where(p => p.SubmittedByLecturerId == lecturerId ||
                db.ProductCoAuthors.Any(a => a.ProductId == p.Id && a.LecturerId == lecturerId))
            .OrderByDescending(p => p.UpdatedAt).ToListAsync(cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken) =>
        await db.Products.AddAsync(product, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}

public sealed class ProductCoAuthorRepository(ApplicationDbContext db) : IProductCoAuthorRepository
{
    public Task<ProductCoAuthor?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        db.ProductCoAuthors.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<List<ProductCoAuthor>> GetByProductIdAsync(long productId, CancellationToken cancellationToken) =>
        db.ProductCoAuthors
            .Where(c => c.ProductId == productId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(long productId, long lecturerId, CancellationToken cancellationToken) =>
        db.ProductCoAuthors.AnyAsync(
            c => c.ProductId == productId && c.LecturerId == lecturerId,
            cancellationToken);

    public async Task AddAsync(ProductCoAuthor coAuthor, CancellationToken cancellationToken) =>
        await db.ProductCoAuthors.AddAsync(coAuthor, cancellationToken);

    public void Remove(ProductCoAuthor coAuthor) =>
        db.ProductCoAuthors.Remove(coAuthor);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}

public sealed class ProductEvidenceRepository(ApplicationDbContext db) : IProductEvidenceRepository
{
    public Task<ProductEvidence?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        db.ProductEvidences.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<List<ProductEvidence>> GetByProductIdAsync(long productId, CancellationToken cancellationToken) =>
        db.ProductEvidences
            .Where(e => e.ProductId == productId)
            .OrderBy(e => e.UploadedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ProductEvidence evidence, CancellationToken cancellationToken) =>
        await db.ProductEvidences.AddAsync(evidence, cancellationToken);

    public void Remove(ProductEvidence evidence) =>
        db.ProductEvidences.Remove(evidence);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}

public sealed class ReviewHistoryRepository(ApplicationDbContext db) : IReviewHistoryRepository
{
    public Task<List<ReviewHistory>> GetByProductIdAsync(long productId, CancellationToken cancellationToken) =>
        db.ReviewHistories
            .Where(h => h.ProductId == productId)
            .OrderBy(h => h.OccurredAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ReviewHistory history, CancellationToken cancellationToken) =>
        await db.ReviewHistories.AddAsync(history, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
