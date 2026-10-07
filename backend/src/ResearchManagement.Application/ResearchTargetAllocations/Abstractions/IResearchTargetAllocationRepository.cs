using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ResearchTargetAllocations.Abstractions;

public interface IResearchTargetAllocationRepository
{
    Task<ResearchTargetAllocation?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken);

    Task<ResearchTargetAllocation?> GetByTargetAndDepartmentAsync(
        long researchTargetId,
        long departmentId,
        CancellationToken cancellationToken);

    Task<List<ResearchTargetAllocation>> GetByResearchTargetIdAsync(
        long researchTargetId,
        CancellationToken cancellationToken);

    Task AddAsync(
        ResearchTargetAllocation allocation,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}