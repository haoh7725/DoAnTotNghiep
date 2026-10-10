using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.ResearchPlans.Abstractions;
using ResearchManagement.Application.ResearchPlans.Models;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class LecturerAccessRepository(
    ApplicationDbContext dbContext) : ILecturerAccessRepository
{
    public async Task<LecturerAccessInfo?> GetAccessInfoAsync(
        long lecturerId,
        CancellationToken cancellationToken)
    {
        return await (
            from lecturer in dbContext.Lecturers
            join department in dbContext.Departments
                on lecturer.DepartmentId equals department.Id
            where lecturer.Id == lecturerId
            select new LecturerAccessInfo(
                lecturer.Id,
                lecturer.AccountId,
                lecturer.DepartmentId,
                department.FacultyId))
            .SingleOrDefaultAsync(cancellationToken);
    }
}