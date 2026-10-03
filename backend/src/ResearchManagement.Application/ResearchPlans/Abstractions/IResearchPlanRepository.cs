using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ResearchPlans.Abstractions;

public interface IResearchPlanRepository
{
    Task<ResearchPlan?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken);

    Task<ResearchPlan?> GetByLecturerAndAcademicYearAsync(
        long lecturerId,
        long academicYearId,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        long lecturerId,
        long academicYearId,
        CancellationToken cancellationToken);

    Task AddAsync(
        ResearchPlan plan,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}