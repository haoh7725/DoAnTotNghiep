using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Application.ResearchPlans.Models;

public sealed class CreateResearchPlanRequest
{
    [Range(1, long.MaxValue)]
    public long LecturerId { get; set; }

    [Range(1, long.MaxValue)]
    public long AcademicYearId { get; set; }

    [Range(0, int.MaxValue)]
    public int CommittedProductCount { get; set; }

    [Required]
    public DateOnly PlanDate { get; set; }

    public DateOnly? Deadline { get; set; }
}

public sealed class UpdateResearchPlanRequest
{
    [Range(0, int.MaxValue)]
    public int CommittedProductCount { get; set; }

    public DateOnly? Deadline { get; set; }
}

public sealed record ResearchPlanResponse(
    long Id,
    long LecturerId,
    long AcademicYearId,
    int CommittedProductCount,
    DateOnly PlanDate,
    string Status,
    DateOnly? Deadline);