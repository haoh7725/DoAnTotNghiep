
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Notifications.Abstractions;
using ResearchManagement.Application.Notifications.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Notifications;

public sealed class NotificationService(
    INotificationRepository notifications,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<NotificationResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();

        var result = await notifications.GetByRecipientAsync(
            accountId,
            cancellationToken);

        return result.Select(ToResponse).ToList();
    }

    public async Task<UnreadNotificationCountResponse> GetUnreadCountAsync(
        CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();

        var count = await notifications.CountUnreadAsync(
            accountId,
            cancellationToken);

        return new UnreadNotificationCountResponse(count);
    }

    public async Task<NotificationResponse> MarkAsReadAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();

        var notification = await notifications.GetByIdForRecipientAsync(
            id,
            accountId,
            cancellationToken)
            ?? throw new NotFoundException(
                "Không tìm thấy thông báo.");

        notification.MarkAsRead();

        await notifications.SaveChangesAsync(cancellationToken);

        return ToResponse(notification);
    }
    public async Task<int> MarkAllAsReadAsync(
        CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();

        var unreadNotifications =
            await notifications.GetUnreadByRecipientAsync(
                accountId,
                cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
        }

        await notifications.SaveChangesAsync(cancellationToken);

        return unreadNotifications.Count;
    }

    private long GetCurrentAccountId()
    {
        return currentUser.Id
            ?? throw new AuthenticationFailedException(
                "Không xác định được người dùng hiện tại.");
    }

    private static NotificationResponse ToResponse(
        Notification notification) => new(
            notification.Id,
            notification.Type,
            notification.Content,
            notification.Channel,
            notification.DeliveryStatus,
            notification.ResearchPlanItemId,
            notification.ResearchPlanId,
            notification.ScientificProductId,
            notification.ScheduledAt,
            notification.SentAt,
            notification.ReadAt,
            notification.IsRead);
}
