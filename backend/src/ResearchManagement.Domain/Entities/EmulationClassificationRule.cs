using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Ngưỡng xếp loại thi đua áp dụng riêng cho một năm học.</summary>
public sealed class EmulationClassificationRule : BaseEntity
{
    private EmulationClassificationRule() { }

    public EmulationClassificationRule(long academicYearId, string name, decimal minimumPercent, int sortOrder)
    {
        Update(academicYearId, name, minimumPercent, sortOrder);
    }

    public long AcademicYearId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal MinimumPercent { get; private set; }
    public int SortOrder { get; private set; }

    public void Update(long academicYearId, string name, decimal minimumPercent, int sortOrder)
    {
        AcademicYearId = academicYearId;
        Name = name.Trim();
        MinimumPercent = minimumPercent;
        SortOrder = sortOrder;
    }
}
