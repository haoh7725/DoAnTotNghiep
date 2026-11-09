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
}
