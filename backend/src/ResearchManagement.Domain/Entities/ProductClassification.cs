using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

public sealed class ProductClassification : BaseEntity
{
    public long ProductTypeId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
