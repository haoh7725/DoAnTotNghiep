using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Api.Contracts;

public sealed record CreateEvaluationRequest(
    [Range(1, long.MaxValue)] long LecturerId,
    [Range(1, long.MaxValue)] long AcademicYearId,
    [Required, RegularExpression("^(GIO|DIEM)$")] string Unit = "GIO");

public sealed record FinalizeEvaluationRequest(
    [RegularExpression("^(GIO|DIEM)$")] string? Unit = null);
