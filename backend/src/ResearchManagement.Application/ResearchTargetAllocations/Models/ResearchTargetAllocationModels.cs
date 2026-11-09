namespace ResearchManagement.Application.ResearchTargetAllocations.Models;

using System.Text.Json.Serialization;

public sealed record CreateResearchTargetAllocationRequest(
    long ResearchTargetId,
    long FacultyId,
    long DepartmentId,
    int AllocatedArticleCount,
    [property: JsonIgnore] DateOnly AllocationDate = default);

public sealed record UpdateResearchTargetAllocationRequest(
    int AllocatedArticleCount,
    [property: JsonIgnore] DateOnly AllocationDate = default);

public sealed record ResearchTargetAllocationResponse(
    long Id,
    long ResearchTargetId,
    long FacultyId,
    long DepartmentId,
    int AllocatedArticleCount,
    DateOnly AllocationDate);
