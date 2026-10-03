using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng ke_hoach_nckh.</summary>
public sealed class ResearchPlan : BaseEntity
{
    private ResearchPlan() { }

    public ResearchPlan(
        long lecturerId,
        long academicYearId,
        int committedProductCount,
        DateOnly planDate,
        DateOnly? deadline = null)
    {
        LecturerId = lecturerId;
        AcademicYearId = academicYearId;
        CommittedProductCount = committedProductCount;
        PlanDate = planDate;
        Deadline = deadline;
        Status = "NHAP";
    }

    public long LecturerId { get; private set; }
    public long AcademicYearId { get; private set; }
    public int CommittedProductCount { get; private set; }
    public DateOnly PlanDate { get; private set; }
    public string Status { get; private set; } = "NHAP";
    public DateOnly? Deadline { get; private set; }

    public void Update(
        int committedProductCount,
        DateOnly? deadline)
    {
        CommittedProductCount = committedProductCount;
        Deadline = deadline;
    }

    public void Submit()
    {
        Status = "DA_DANG_KY";
    }
}