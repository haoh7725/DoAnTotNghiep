using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Api.Contracts;

public sealed record CreateEvaluationRequest(
    [Range(1, long.MaxValue)] long LecturerId,
    [Range(1, long.MaxValue)] long AcademicYearId);

public sealed record FinalizeEvaluationRequest(
    [Required, StringLength(100)] string Classification,
    [Required] string ClassificationBasis);
