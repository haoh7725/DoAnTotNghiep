namespace ResearchManagement.Application.ResearchTargets.Models;

public sealed record CreateResearchTargetRequest(
    long FacultyId,
    long AcademicYearId,
    long ProductTypeId,
    int AssignedArticleCount,
    DateOnly? Deadline,
    long? JournalConferenceId = null);

public sealed record UpdateResearchTargetRequest(
    long ProductTypeId,
    int AssignedArticleCount,
    DateOnly? Deadline,
    long? JournalConferenceId = null);

public sealed record ResearchTargetResponse(
    long Id,
    long FacultyId,
    long AcademicYearId,
    long ProductTypeId,
    int AssignedArticleCount,
    DateOnly? Deadline,
    long? JournalConferenceId,
    string Status,
    long? AssignedByAccountId,
    long? AcceptedByAccountId,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? LockedAt);
