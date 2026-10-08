using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng chi_tieu_nckh.</summary>
public sealed class ResearchTarget : BaseEntity
{
    private ResearchTarget() { }

    public ResearchTarget(
        long facultyId,
        long academicYearId,
        int assignedArticleCount,
        DateOnly? deadline)
    {
        if (assignedArticleCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(assignedArticleCount),
                "Số bài được giao không được âm.");
        }

        FacultyId = facultyId;
        AcademicYearId = academicYearId;
        AssignedArticleCount = assignedArticleCount;
        Deadline = deadline;
    }

    public long FacultyId { get; private set; }

    public long AcademicYearId { get; private set; }

    public int AssignedArticleCount { get; private set; }

    public DateOnly? Deadline { get; private set; }

    public void Update(
        int assignedArticleCount,
        DateOnly? deadline)
    {
        if (assignedArticleCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(assignedArticleCount),
                "Số bài được giao không được âm.");
        }

        AssignedArticleCount = assignedArticleCount;
        Deadline = deadline;
    }
}