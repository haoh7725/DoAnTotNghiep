using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Plagiarism.Abstractions;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class PlagiarismCheckRepository(ApplicationDbContext db) : IPlagiarismCheckRepository
{
    public Task<PlagiarismCheck?> GetByIdAsync(long id, CancellationToken ct) =>
        db.PlagiarismChecks.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<PlagiarismCheck?> GetByExternalScanIdAsync(string scanId, CancellationToken ct) =>
        db.PlagiarismChecks.FirstOrDefaultAsync(x => x.ExternalScanId == scanId, ct);

    public Task<List<PlagiarismCheck>> GetByProductIdAsync(long productId, CancellationToken ct) =>
        db.PlagiarismChecks.Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public Task<bool> HasActiveCheckAsync(long evidenceId, string fileHash, CancellationToken ct) =>
        db.PlagiarismChecks.AnyAsync(x => x.EvidenceId == evidenceId && x.FileHash == fileHash &&
            (x.Status == PlagiarismCheckStatuses.Pending || x.Status == PlagiarismCheckStatuses.Sending ||
             x.Status == PlagiarismCheckStatuses.Processing), ct);

    public async Task AddAsync(PlagiarismCheck item, CancellationToken ct) =>
        await db.PlagiarismChecks.AddAsync(item, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
