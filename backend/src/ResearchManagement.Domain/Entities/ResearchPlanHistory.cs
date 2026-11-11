using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

public sealed class ResearchPlanHistory : BaseEntity
{
    private ResearchPlanHistory() { }

    public ResearchPlanHistory(long researchPlanId, long actorAccountId,
        string fromStatus, string toStatus, string reason)
    {
        ResearchPlanId = researchPlanId;
        ActorAccountId = actorAccountId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Reason = reason.Trim();
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public long ResearchPlanId { get; private set; }
    public long ActorAccountId { get; private set; }
    public string FromStatus { get; private set; } = string.Empty;
    public string ToStatus { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
}
