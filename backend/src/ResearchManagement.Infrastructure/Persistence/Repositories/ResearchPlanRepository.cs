using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.ResearchPlans.Abstractions;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class ResearchPlanRepository(ApplicationDbContext db)
    : IResearchPlanRepository
{
    public Task<ResearchPlan?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken) =>
        db.ResearchPlans
            .FirstOrDefaultAsync(
                plan => plan.Id == id,
                cancellationToken);

    public Task<ResearchPlan?> GetByLecturerAndAcademicYearAsync(
        long lecturerId,
        long academicYearId,
        CancellationToken cancellationToken) =>
        db.ResearchPlans
            .FirstOrDefaultAsync(
                plan =>
                    plan.LecturerId == lecturerId &&
                    plan.AcademicYearId == academicYearId,
                cancellationToken);

    public Task<bool> ExistsAsync(
        long lecturerId,
        long academicYearId,
        CancellationToken cancellationToken) =>
        db.ResearchPlans
            .AnyAsync(
                plan =>
                    plan.LecturerId == lecturerId &&
                    plan.AcademicYearId == academicYearId,
                cancellationToken);

    public async Task AddAsync(
        ResearchPlan plan,
        CancellationToken cancellationToken) =>
        await db.ResearchPlans.AddAsync(plan, cancellationToken);

    public Task SaveChangesAsync(
        CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}