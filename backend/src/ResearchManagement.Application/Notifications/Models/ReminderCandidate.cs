
namespace ResearchManagement.Application.Notifications.Models;

public sealed record ReminderCandidate(
    long RecipientAccountId,
    string ItemName,
    DateOnly Deadline,
    string DeduplicationKey,
    long? ResearchPlanItemId = null,
    long? ResearchPlanId = null);
