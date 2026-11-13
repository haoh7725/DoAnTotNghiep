using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(ApplicationDbContext db) : IProductRepository
{
    public async Task<bool> HasDuplicateWorkAsync(Product product, CancellationToken ct)
    {
        var normalizedTitle = System.Text.RegularExpressions.Regex.Replace(product.Title.Trim(), @"\s+", " ").ToLowerInvariant();
        var candidates = await (from other in db.Products
                join item in db.ResearchPlanItems on other.ResearchPlanItemId equals item.Id
                where other.Id != product.Id &&
                    (product.WorkIdentifier != null && other.WorkIdentifier == product.WorkIdentifier ||
                     (product.WorkIdentifier == null || other.WorkIdentifier == null) &&
                     other.Status == "DA_DUYET" && product.PublishedDate != null && other.PublishedDate == product.PublishedDate &&
                     db.ResearchPlanItems.Any(own => own.Id == product.ResearchPlanItemId && own.ProductTypeId == item.ProductTypeId))
                select new { other.WorkIdentifier, other.Title }).ToListAsync(ct);
        return candidates.Any(other => product.WorkIdentifier != null && other.WorkIdentifier == product.WorkIdentifier ||
            System.Text.RegularExpressions.Regex.Replace(other.Title.Trim(), @"\s+", " ").ToLowerInvariant() == normalizedTitle);
    }

    public Task<int> CountByTargetAndLecturerAsync(long targetId, long lecturerId, CancellationToken cancellationToken) =>
        db.Products.CountAsync(p => p.ResearchTargetId == targetId && p.SubmittedByLecturerId == lecturerId, cancellationToken);

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

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        var histories = db.ChangeTracker.Entries<ReviewHistory>()
            .Where(e => e.State == EntityState.Added).Select(e => e.Entity).ToList();
        if (histories.Count == 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        IDbContextTransaction? ownedTransaction = null;
        if (db.Database.CurrentTransaction is null)
            ownedTransaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var history in histories.OrderBy(h => h.ProductId))
            {
                // Serialize version allocation for this product across concurrent requests.
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock({history.ProductId})", cancellationToken);
                var product = db.Products.Local.Single(p => p.Id == history.ProductId);
                var version = await db.ProductSnapshots.Where(s => s.ProductId == product.Id)
                    .MaxAsync(s => (int?)s.Version, cancellationToken) ?? 0;
                db.ProductSnapshots.Add(new ProductSnapshot(product, history.ActorAccountId, version + 1));
            }
            await db.SaveChangesAsync(cancellationToken);
            if (ownedTransaction is not null)
                await ownedTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            if (ownedTransaction is not null)
                await ownedTransaction.DisposeAsync();
        }
    }
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
