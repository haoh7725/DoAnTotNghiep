
using ResearchManagement.Application.Notifications.Abstractions;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Notifications;

public sealed class ReminderService(
    IReminderRepository reminders,
    INotificationRepository notifications)
{
    public async Task<int> GenerateAsync(
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var candidates = await reminders.GetCandidatesAsync(
            today,
            cancellationToken);

        var createdCount = 0;

        foreach (var item in candidates)
        {
            var type = item.Deadline < today
                ? "QUA_HAN"
                : "NHAC_HAN";

            var deduplicationKey =
                $"{type}:ITEM:{item.ResearchPlanItemId}:{today:yyyyMMdd}";

            var exists = await notifications.ExistsAsync(
                item.RecipientAccountId,
                "IN_APP",
                deduplicationKey,
                cancellationToken);

            if (exists)
                continue;

            var content = type == "QUA_HAN"
                ? $"Nội dung '{item.ItemName}' đã quá hạn ngày {item.Deadline:dd/MM/yyyy}. Vui lòng cập nhật tiến độ."
                : $"Nội dung '{item.ItemName}' sắp đến hạn ngày {item.Deadline:dd/MM/yyyy}. Vui lòng kiểm tra tiến độ.";

            var notification = new Notification(
                recipientAccountId: item.RecipientAccountId,
                type: type,
                content: content,
                deduplicationKey: deduplicationKey,
                researchPlanItemId: item.ResearchPlanItemId,
                researchPlanId: item.ResearchPlanId);

            notification.MarkAsSent();

            await notifications.AddAsync(
                notification,
                cancellationToken);

            var saved = await notifications.SaveChangesAsync(
                cancellationToken);

            if (saved)
            {
                createdCount++;
            }
        }

        return createdCount;
    }
}
