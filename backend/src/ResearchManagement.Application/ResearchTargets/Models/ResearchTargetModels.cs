namespace ResearchManagement.Application.ResearchTargets.Models;

public sealed record CreateResearchTargetRequest(
    long FacultyId,
    long AcademicYearId,
    int AssignedArticleCount,
    DateOnly? Deadline);

public sealed record UpdateResearchTargetRequest(
    int AssignedArticleCount,
    DateOnly? Deadline);

public sealed record ResearchTargetResponse(
    long Id,
    long FacultyId,
    long AcademicYearId,
    int AssignedArticleCount,
    DateOnly? Deadline);