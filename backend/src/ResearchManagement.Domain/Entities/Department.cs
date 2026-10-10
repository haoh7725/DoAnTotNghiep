using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng bo_mon.</summary>
public sealed class Department : BaseEntity
{
    public Department() { }

    public long FacultyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}