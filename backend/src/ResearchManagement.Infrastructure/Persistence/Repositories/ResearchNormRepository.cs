using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.ResearchNorms.Abstractions;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class ResearchNormRepository(
    ApplicationDbContext dbContext) : IResearchNormRepository
{
    public Task<ResearchNorm?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken) =>
        dbContext.ResearchNorms
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

    public Task<ResearchNorm?> GetByLecturerAndAcademicYearAsync(
        long lecturerId,
        long academicYearId,
        CancellationToken cancellationToken) =>
        dbContext.ResearchNorms
            .FirstOrDefaultAsync(
                x => x.LecturerId == lecturerId &&
                     x.AcademicYearId == academicYearId,
                cancellationToken);

    public Task<List<ResearchNorm>> GetAllAsync(
        CancellationToken cancellationToken) =>
        dbContext.ResearchNorms
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        ResearchNorm researchNorm,
        CancellationToken cancellationToken)
    {
        await dbContext.ResearchNorms.AddAsync(
            researchNorm,
            cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}