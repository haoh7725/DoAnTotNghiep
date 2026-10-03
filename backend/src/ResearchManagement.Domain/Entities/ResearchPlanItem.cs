using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng noi_dung_ke_hoach.</summary>
public sealed class ResearchPlanItem : BaseEntity
{
    private ResearchPlanItem() { }

    public ResearchPlanItem(
        long researchPlanId,
        long productTypeId,
        string name,
        int committedQuantity,
        DateOnly? deadline = null)
    {
        ResearchPlanId = researchPlanId;
        ProductTypeId = productTypeId;
        Name = name.Trim();
        CommittedQuantity = committedQuantity;
        Deadline = deadline;
        Status = "CHUA_THUC_HIEN";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public long ResearchPlanId { get; private set; }
    public long ProductTypeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int CommittedQuantity { get; private set; }
    public DateOnly? Deadline { get; private set; }
    public string Status { get; private set; } = "CHUA_THUC_HIEN";
    public string? Result { get; private set; }
    public string? DifficultyProposal { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(
        string name,
        int committedQuantity,
        DateOnly? deadline)
    {
        Name = name.Trim();
        CommittedQuantity = committedQuantity;
        Deadline = deadline;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateProgress(
        string status,
        string? result,
        string? difficultyProposal)
    {
        Status = status;
        Result = result;
        DifficultyProposal = difficultyProposal;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}