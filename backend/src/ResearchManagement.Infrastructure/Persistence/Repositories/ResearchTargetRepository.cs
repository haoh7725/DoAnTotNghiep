using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.ResearchTargets.Abstractions;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class ResearchTargetRepository(ApplicationDbContext dbContext)
    : IResearchTargetRepository
{
    public Task<ResearchTarget?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken)
    {
        return dbContext.ResearchTargets
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public Task<ResearchTarget?> GetByFacultyAndAcademicYearAsync(
        long facultyId,
        long academicYearId,
        CancellationToken cancellationToken)
    {
        return dbContext.ResearchTargets
            .FirstOrDefaultAsync(
                x => x.FacultyId == facultyId &&
                     x.AcademicYearId == academicYearId,
                cancellationToken);
    }

    public Task<List<ResearchTarget>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.ResearchTargets
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(
        ResearchTarget researchTarget,
        CancellationToken cancellationToken)
    {
        return dbContext.ResearchTargets
            .AddAsync(researchTarget, cancellationToken)
            .AsTask();
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}