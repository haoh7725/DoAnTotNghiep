using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng lich_su_tien_do.</summary>
public sealed class ProgressHistory : BaseEntity
{
    private ProgressHistory() { }

    public ProgressHistory(
        long researchPlanItemId,
        long updatedByAccountId,
        string status,
        string? result,
        string? difficultyProposal)
    {
        ResearchPlanItemId = researchPlanItemId;
        UpdatedByAccountId = updatedByAccountId;
        Status = status;
        Result = result;
        DifficultyProposal = difficultyProposal;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public long ResearchPlanItemId { get; private set; }

    public long UpdatedByAccountId { get; private set; }

    public string Status { get; private set; } = string.Empty;

    public string? Result { get; private set; }

    public string? DifficultyProposal { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }
}