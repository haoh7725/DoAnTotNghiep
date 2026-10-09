
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Notifications.Abstractions;
using ResearchManagement.Application.Notifications.Models;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class ReminderRepository(ApplicationDbContext db)
    : IReminderRepository
{
    public Task<List<ReminderCandidate>> GetCandidatesAsync(
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var reminderLimit = today.AddDays(3);

        var query =
            from item in db.ResearchPlanItems.AsNoTracking()
            join plan in db.ResearchPlans.AsNoTracking()
                on item.ResearchPlanId equals plan.Id
            join lecturer in db.Lecturers.AsNoTracking()
                on plan.LecturerId equals lecturer.Id
            where plan.Status == "DA_DANG_KY"
                && item.Deadline.HasValue
                && item.Deadline.Value <= reminderLimit
                && item.Status != "DA_THUC_HIEN"
            select new ReminderCandidate(
                item.Id,
                plan.Id,
                lecturer.AccountId,
                item.Name,
                item.Deadline!.Value,
                item.Status);

        return query.ToListAsync(cancellationToken);
    }
}
