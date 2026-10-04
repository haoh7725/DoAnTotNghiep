using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.ResearchPlans.Abstractions;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class ProgressHistoryRepository(
    ApplicationDbContext dbContext) : IProgressHistoryRepository
{
    public Task<List<ProgressHistory>> GetByResearchPlanItemIdAsync(
        long researchPlanItemId,
        CancellationToken cancellationToken) =>
        dbContext.ProgressHistories
            .Where(history =>
                history.ResearchPlanItemId == researchPlanItemId)
            .OrderByDescending(history => history.UpdatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        ProgressHistory history,
        CancellationToken cancellationToken)
    {
        await dbContext.ProgressHistories.AddAsync(
            history,
            cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}