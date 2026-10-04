namespace ResearchManagement.Domain.Entities;
public sealed class Department
{
    public long Id { get; set; }
    public long FacultyId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}
