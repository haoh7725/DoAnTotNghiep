namespace ResearchManagement.Domain.Entities;
public sealed class AcademicYear
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
