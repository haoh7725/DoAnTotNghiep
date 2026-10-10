using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ResearchNorms.Abstractions;

public interface IResearchNormRepository
{
    Task<ResearchNorm?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken);

    Task<ResearchNorm?> GetByLecturerAndAcademicYearAsync(
        long lecturerId,
        long academicYearId,
        CancellationToken cancellationToken);

    Task<List<ResearchNorm>> GetAllAsync(
        CancellationToken cancellationToken);

    Task AddAsync(
        ResearchNorm researchNorm,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}