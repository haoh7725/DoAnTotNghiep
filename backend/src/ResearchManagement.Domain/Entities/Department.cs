using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng bo_mon.</summary>
public sealed class Department : BaseEntity
{
    private Department() { }

    public long FacultyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
}