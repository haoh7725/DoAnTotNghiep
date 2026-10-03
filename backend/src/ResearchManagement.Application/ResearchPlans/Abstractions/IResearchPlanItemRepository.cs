using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ResearchPlans.Abstractions;

public interface IResearchPlanItemRepository
{
    Task<ResearchPlanItem?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken);

    Task<List<ResearchPlanItem>> GetByResearchPlanIdAsync(
        long researchPlanId,
        CancellationToken cancellationToken);

    Task AddAsync(
        ResearchPlanItem item,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}