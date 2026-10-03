using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Application.ResearchPlans.Models;

public sealed class CreateResearchPlanItemRequest
{
    [Required]
    public long ProductTypeId { get; set; }

    [Required, StringLength(500)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CommittedQuantity { get; set; } = 1;

    public DateOnly? Deadline { get; set; }
}

public sealed class UpdateResearchPlanItemRequest
{
    [Required, StringLength(500)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CommittedQuantity { get; set; }

    public DateOnly? Deadline { get; set; }
}

public sealed class UpdateResearchPlanItemProgressRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;

    public string? Result { get; set; }

    public string? DifficultyProposal { get; set; }
}

public sealed record ResearchPlanItemResponse(
    long Id,
    long ResearchPlanId,
    long ProductTypeId,
    string Name,
    int CommittedQuantity,
    DateOnly? Deadline,
    string Status,
    string? Result,
    string? DifficultyProposal,
    DateTimeOffset UpdatedAt);

public sealed record ProgressHistoryResponse(
    long Id,
    long ResearchPlanItemId,
    long UpdatedByAccountId,
    string Status,
    string? Result,
    string? DifficultyProposal,
    DateTimeOffset UpdatedAt);