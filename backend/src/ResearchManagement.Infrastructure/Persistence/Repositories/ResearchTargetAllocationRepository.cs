using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.ResearchTargetAllocations.Abstractions;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class ResearchTargetAllocationRepository(
    ApplicationDbContext dbContext)
    : IResearchTargetAllocationRepository
{
    public Task<ResearchTargetAllocation?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken) =>
        dbContext.ResearchTargetAllocations
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

    public Task<ResearchTargetAllocation?> GetByTargetAndDepartmentAsync(
        long researchTargetId,
        long departmentId,
        CancellationToken cancellationToken) =>
        dbContext.ResearchTargetAllocations
            .FirstOrDefaultAsync(
                x => x.ResearchTargetId == researchTargetId &&
                     x.DepartmentId == departmentId,
                cancellationToken);

    public Task<List<ResearchTargetAllocation>> GetByResearchTargetIdAsync(
        long researchTargetId,
        CancellationToken cancellationToken) =>
        dbContext.ResearchTargetAllocations
            .Where(x => x.ResearchTargetId == researchTargetId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task AddAsync(
        ResearchTargetAllocation allocation,
        CancellationToken cancellationToken) =>
        dbContext.ResearchTargetAllocations
            .AddAsync(allocation, cancellationToken)
            .AsTask();

    public Task SaveChangesAsync(
        CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}