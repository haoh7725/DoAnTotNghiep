using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng phan_bo_chi_tieu.</summary>
public sealed class ResearchTargetAllocation : BaseEntity
{
    private ResearchTargetAllocation() { }

    public ResearchTargetAllocation(
        long researchTargetId,
        long facultyId,
        long departmentId,
        int allocatedArticleCount,
        DateOnly allocationDate)
    {
        if (allocatedArticleCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(allocatedArticleCount),
                "Số bài phân bổ không được âm.");
        }

        ResearchTargetId = researchTargetId;
        FacultyId = facultyId;
        DepartmentId = departmentId;
        AllocatedArticleCount = allocatedArticleCount;
        AllocationDate = allocationDate;
    }

    public long ResearchTargetId { get; private set; }

    public long FacultyId { get; private set; }

    public long DepartmentId { get; private set; }

    public int AllocatedArticleCount { get; private set; }

    public DateOnly AllocationDate { get; private set; }

    public void Update(
        int allocatedArticleCount,
        DateOnly allocationDate)
    {
        if (allocatedArticleCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(allocatedArticleCount),
                "Số bài phân bổ không được âm.");
        }

        AllocatedArticleCount = allocatedArticleCount;
        AllocationDate = allocationDate;
    }
}