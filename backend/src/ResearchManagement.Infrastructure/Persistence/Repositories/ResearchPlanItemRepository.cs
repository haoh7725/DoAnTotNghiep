using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.ResearchPlans.Abstractions;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class ResearchPlanItemRepository(ApplicationDbContext db)
    : IResearchPlanItemRepository
{
    public Task<ResearchPlanItem?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken) =>
        db.ResearchPlanItems
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken);

    public Task<List<ResearchPlanItem>> GetByResearchPlanIdAsync(
        long researchPlanId,
        CancellationToken cancellationToken) =>
        db.ResearchPlanItems
            .Where(item => item.ResearchPlanId == researchPlanId)
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        ResearchPlanItem item,
        CancellationToken cancellationToken) =>
        await db.ResearchPlanItems.AddAsync(item, cancellationToken);

    public Task SaveChangesAsync(
        CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}