namespace ResearchManagement.Application.ResearchNorms.Models;

public sealed record CreateResearchNormRequest(
    long LecturerId,
    long AcademicYearId,
    decimal RequiredHours,
    string? Basis,
    decimal RequiredPoints = 0m);

public sealed record UpdateResearchNormRequest(
    decimal RequiredHours,
    string? Basis,
    decimal RequiredPoints = 0m);

public sealed record ResearchNormResponse(
    long Id,
    long LecturerId,
    long AcademicYearId,
    decimal RequiredHours,
    decimal RequiredPoints,
    string? Basis,
    string? LecturerCode = null,
    string? LecturerName = null);
