using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng giang_vien.</summary>
public sealed class Lecturer : BaseEntity
{
    private Lecturer() { }

    public long DepartmentId { get; private set; }
    public long AccountId { get; private set; }
    public string LecturerCode { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
}