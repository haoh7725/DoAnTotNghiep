
namespace ResearchManagement.Application.Notifications.Models;

public sealed record NotificationResponse(
    long Id,
    string Type,
    string Content,
    string Channel,
    string DeliveryStatus,
    long? ResearchPlanItemId,
    long? ResearchPlanId,
    long? ScientificProductId,
    DateTimeOffset ScheduledAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? ReadAt,
    bool IsRead);

public sealed record UnreadNotificationCountResponse(
    int UnreadCount);
