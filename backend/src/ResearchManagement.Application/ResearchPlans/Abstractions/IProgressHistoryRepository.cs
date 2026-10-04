using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ResearchPlans.Abstractions;

public interface IProgressHistoryRepository
{
    Task<List<ProgressHistory>> GetByResearchPlanItemIdAsync(
        long researchPlanItemId,
        CancellationToken cancellationToken);

    Task AddAsync(
        ProgressHistory history,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}