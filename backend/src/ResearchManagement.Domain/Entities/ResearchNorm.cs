using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng dinh_muc_nckh.</summary>
public sealed class ResearchNorm : BaseEntity
{
    private ResearchNorm() { }

    public ResearchNorm(
        long lecturerId,
        long academicYearId,
        decimal requiredHours,
        string? basis)
    {
        if (requiredHours < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredHours),
                "Số giờ định mức không được âm.");
        }

        LecturerId = lecturerId;
        AcademicYearId = academicYearId;
        RequiredHours = requiredHours;
        Basis = basis;
    }

    public long LecturerId { get; private set; }

    public long AcademicYearId { get; private set; }

    public decimal RequiredHours { get; private set; }

    public string? Basis { get; private set; }

    public void Update(
        decimal requiredHours,
        string? basis)
    {
        if (requiredHours < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredHours),
                "Số giờ định mức không được âm.");
        }

        RequiredHours = requiredHours;
        Basis = basis;
    }
}