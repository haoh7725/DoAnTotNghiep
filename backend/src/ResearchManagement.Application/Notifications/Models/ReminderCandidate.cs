
namespace ResearchManagement.Application.Notifications.Models;

public sealed record ReminderCandidate(
    long ResearchPlanItemId,
    long ResearchPlanId,
    long RecipientAccountId,
    string ItemName,
    DateOnly Deadline,
    string Status);
