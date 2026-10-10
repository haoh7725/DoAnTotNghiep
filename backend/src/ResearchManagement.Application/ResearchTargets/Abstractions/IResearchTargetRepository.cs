using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ResearchTargets.Abstractions;

public interface IResearchTargetRepository
{
    Task<ResearchTarget?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken);

    Task<ResearchTarget?> GetByFacultyAndAcademicYearAsync(
        long facultyId,
        long academicYearId,
        CancellationToken cancellationToken);

    Task<List<ResearchTarget>> GetAllAsync(
        CancellationToken cancellationToken);

    Task AddAsync(
        ResearchTarget researchTarget,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}