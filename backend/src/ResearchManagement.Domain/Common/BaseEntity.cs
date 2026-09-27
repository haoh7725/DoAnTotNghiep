namespace ResearchManagement.Domain.Common;

public abstract class BaseEntity
{
    public long Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; protected set; }
}
