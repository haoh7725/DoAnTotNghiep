namespace ResearchManagement.Application.ResearchNorms.Models;

public sealed record CreateResearchNormRequest(
    long LecturerId,
    long AcademicYearId,
    decimal RequiredHours,
    string? Basis);

public sealed record UpdateResearchNormRequest(
    decimal RequiredHours,
    string? Basis);

public sealed record ResearchNormResponse(
    long Id,
    long LecturerId,
    long AcademicYearId,
    decimal RequiredHours,
    string? Basis);