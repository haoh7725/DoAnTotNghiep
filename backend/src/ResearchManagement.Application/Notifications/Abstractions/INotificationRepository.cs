
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Notifications.Abstractions;

public interface INotificationRepository
{
    Task<Notification?> GetByIdForRecipientAsync(
        long id,
        long recipientAccountId,
        CancellationToken cancellationToken);

    Task<List<Notification>> GetByRecipientAsync(
        long recipientAccountId,
        CancellationToken cancellationToken);

    Task<int> CountUnreadAsync(
        long recipientAccountId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken);

    Task<bool> SaveChangesAsync(
        CancellationToken cancellationToken);

    Task<List<Notification>> GetUnreadByRecipientAsync(
        long recipientAccountId,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        long recipientAccountId,
        string channel,
        string deduplicationKey,
        CancellationToken cancellationToken);
}
