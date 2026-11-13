using Microsoft.EntityFrameworkCore;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Infrastructure;

/// <summary>Tạo thông báo theo vai trò và phạm vi đơn vị trong cùng transaction nghiệp vụ.</summary>
public sealed class WorkflowNotificationDispatcher(ApplicationDbContext db)
{
    public async Task QueueForRoleAsync(
        string role,
        string content,
        string deduplicationKey,
        string actionPath,
        long? facultyId,
        long? departmentId,
        long? productId,
        CancellationToken ct,
        long? excludeAccountId = null)
    {
        var query = from assignment in db.RoleAssignments.AsNoTracking()
                    join account in db.Accounts.AsNoTracking() on assignment.AccountId equals account.Id
                    where assignment.Role == role && account.Status == AccountStatuses.Active &&
                        (!productId.HasValue || !db.Lecturers.Any(lecturer => lecturer.AccountId == account.Id &&
                            (db.Products.Any(product => product.Id == productId && product.SubmittedByLecturerId == lecturer.Id) ||
                             db.ProductCoAuthors.Any(author => author.ProductId == productId && author.LecturerId == lecturer.Id))))
                    select new
                    {
                        assignment.AccountId,
                        assignment.FacultyId,
                        assignment.DepartmentId
                    };

        if (role == Roles.DepartmentHead)
            query = query.Where(x => x.FacultyId == facultyId && x.DepartmentId == departmentId);
        else if (role == Roles.FacultyDean)
            query = query.Where(x => x.FacultyId == facultyId);

        var recipients = await query.Select(x => x.AccountId).Distinct().ToListAsync(ct);
        foreach (var accountId in recipients.Where(x => x != excludeAccountId))
            await QueueForAccountAsync(accountId, content, $"{deduplicationKey}:{accountId}", actionPath, productId, ct);
    }

    public async Task QueueForAccountAsync(
        long accountId,
        string content,
        string deduplicationKey,
        string actionPath,
        long? productId,
        CancellationToken ct)
    {
        if (await db.Notifications.AnyAsync(x =>
                x.RecipientAccountId == accountId &&
                x.Channel == "IN_APP" &&
                x.DeduplicationKey == deduplicationKey, ct))
            return;

        var notification = new Notification(
            accountId,
            "HE_THONG",
            content,
            deduplicationKey,
            scientificProductId: productId,
            actionPath: actionPath);
        notification.MarkAsSent();
        db.Notifications.Add(notification);
    }
}
