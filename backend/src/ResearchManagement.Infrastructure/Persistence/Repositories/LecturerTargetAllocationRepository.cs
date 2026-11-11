using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.ResearchTargetAllocations.Abstractions;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class LecturerTargetAllocationRepository(ApplicationDbContext db) : ILecturerTargetAllocationRepository
{
    public Task<bool> ExistsAsync(long departmentAllocationId, long lecturerId, CancellationToken cancellationToken) =>
        db.LecturerTargetAllocations.AnyAsync(x => x.DepartmentAllocationId == departmentAllocationId && x.LecturerId == lecturerId, cancellationToken);

    public Task<int?> GetAllocatedArticleCountAsync(long departmentAllocationId, long lecturerId, CancellationToken cancellationToken) =>
        db.LecturerTargetAllocations.Where(x => x.DepartmentAllocationId == departmentAllocationId && x.LecturerId == lecturerId)
            .Select(x => (int?)x.AllocatedArticleCount).SingleOrDefaultAsync(cancellationToken);

    public Task<int> GetTotalAllocatedArticleCountAsync(long lecturerId, long academicYearId, CancellationToken cancellationToken) =>
        (from personal in db.LecturerTargetAllocations
         join allocation in db.ResearchTargetAllocations on personal.DepartmentAllocationId equals allocation.Id
         join target in db.ResearchTargets on allocation.ResearchTargetId equals target.Id
         where personal.LecturerId == lecturerId && target.AcademicYearId == academicYearId && target.Status != "DA_HUY"
         select personal.AllocatedArticleCount).SumAsync(cancellationToken);
}
