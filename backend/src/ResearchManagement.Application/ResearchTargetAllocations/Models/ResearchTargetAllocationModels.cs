namespace ResearchManagement.Application.ResearchTargetAllocations.Models;

public sealed record CreateResearchTargetAllocationRequest(
    long ResearchTargetId,
    long FacultyId,
    long DepartmentId,
    int AllocatedArticleCount,
    DateOnly AllocationDate);

public sealed record UpdateResearchTargetAllocationRequest(
    int AllocatedArticleCount,
    DateOnly AllocationDate);

public sealed record ResearchTargetAllocationResponse(
    long Id,
    long ResearchTargetId,
    long FacultyId,
    long DepartmentId,
    int AllocatedArticleCount,
    DateOnly AllocationDate);