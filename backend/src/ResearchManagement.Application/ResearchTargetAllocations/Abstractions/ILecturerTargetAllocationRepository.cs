namespace ResearchManagement.Application.ResearchTargetAllocations.Abstractions;

public interface ILecturerTargetAllocationRepository
{
    Task<bool> ExistsAsync(long departmentAllocationId, long lecturerId, CancellationToken cancellationToken);
    Task<int?> GetAllocatedArticleCountAsync(long departmentAllocationId, long lecturerId, CancellationToken cancellationToken);
    Task<int> GetTotalAllocatedArticleCountAsync(long lecturerId, long academicYearId, CancellationToken cancellationToken);
}
