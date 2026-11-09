using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

public sealed class LecturerTargetAllocation : BaseEntity
{
    private LecturerTargetAllocation() { }
    public LecturerTargetAllocation(long departmentAllocationId, long lecturerId, int allocatedArticleCount, DateOnly allocationDate)
    {
        if (allocatedArticleCount <= 0) throw new ArgumentOutOfRangeException(nameof(allocatedArticleCount), "Số bài giao phải lớn hơn 0.");
        DepartmentAllocationId = departmentAllocationId;
        LecturerId = lecturerId;
        AllocatedArticleCount = allocatedArticleCount;
        AllocationDate = allocationDate;
    }
    public long DepartmentAllocationId { get; private set; }
    public long LecturerId { get; private set; }
    public int AllocatedArticleCount { get; private set; }
    public DateOnly AllocationDate { get; private set; }
    public void Update(int count, DateOnly date)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Số bài giao phải lớn hơn 0.");
        AllocatedArticleCount = count;
        AllocationDate = date;
    }
}
