
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Notifications.Abstractions;
using ResearchManagement.Domain.Entities;
using Npgsql;
namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(ApplicationDbContext db)
    : INotificationRepository
{
    public Task<Notification?> GetByIdForRecipientAsync(
        long id,
        long recipientAccountId,
        CancellationToken cancellationToken)
    {
        return db.Notifications.FirstOrDefaultAsync(
            x => x.Id == id &&
                 x.RecipientAccountId == recipientAccountId,
            cancellationToken);
    }

    public Task<List<Notification>> GetByRecipientAsync(
        long recipientAccountId,
        CancellationToken cancellationToken)
    {
        return db.Notifications
            .AsNoTracking()
            .Where(x => x.RecipientAccountId == recipientAccountId)
            .OrderByDescending(x => x.ScheduledAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountUnreadAsync(
        long recipientAccountId,
        CancellationToken cancellationToken)
    {
        return db.Notifications.CountAsync(
            x => x.RecipientAccountId == recipientAccountId &&
                 x.ReadAt == null,
            cancellationToken);
    }

    public async Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        await db.Notifications.AddAsync(
            notification,
            cancellationToken);
    }

    public async Task<bool> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pgEx
                && pgEx.SqlState == PostgresErrorCodes.UniqueViolation
                && pgEx.ConstraintName ==
                    "thong_bao_nguoi_nhan_id_kenh_khoa_chong_trung_key")
        {
            foreach (var entry in ex.Entries)
            {
                entry.State = EntityState.Detached;
            }

            return false;
        }
    }
    public Task<List<Notification>> GetUnreadByRecipientAsync(
        long recipientAccountId,
        CancellationToken cancellationToken)
    {
        return db.Notifications
            .Where(x => x.RecipientAccountId == recipientAccountId &&
                        x.ReadAt == null)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(
        long recipientAccountId,
        string channel,
        string deduplicationKey,
        CancellationToken cancellationToken)
    {
        return db.Notifications.AnyAsync(
            x => x.RecipientAccountId == recipientAccountId
                && x.Channel == channel
                && x.DeduplicationKey == deduplicationKey,
            cancellationToken);
    }
}
