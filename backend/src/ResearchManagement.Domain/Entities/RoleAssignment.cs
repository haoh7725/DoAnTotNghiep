namespace ResearchManagement.Domain.Entities;
public sealed class RoleAssignment
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string Role { get; set; } = "";
    public string Scope { get; set; } = "";
    public long? FacultyId { get; set; }
    public long? DepartmentId { get; set; }
}
