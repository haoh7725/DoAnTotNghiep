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
    public async Task<Dictionary<long, (string Code, string FullName)>> GetLecturerNamesAsync(
    IReadOnlyCollection<long> lecturerIds,
    CancellationToken cancellationToken)
    {
        if (lecturerIds.Count == 0)
            return new Dictionary<long, (string Code, string FullName)>();

        var lecturers = await dbContext.Lecturers
            .AsNoTracking()
            .Where(x => lecturerIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Code, x.FullName })
            .ToListAsync(cancellationToken);

        return lecturers.ToDictionary(
            x => x.Id,
            x => (x.Code, x.FullName));
    }
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