
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Notifications.Abstractions;
using ResearchManagement.Application.Notifications.Models;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class ReminderRepository(ApplicationDbContext db)
    : IReminderRepository
{
    public async Task<List<ReminderCandidate>> GetCandidatesAsync(
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var reminderLimit = today.AddDays(3);

        var planItemsQuery =
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
                lecturer.AccountId, item.Name, item.Deadline!.Value,
                "ITEM:" + item.Id, item.Id, plan.Id);

        var assignedTargetsQuery =
            from assignment in db.LecturerTargetAllocations.AsNoTracking()
            join allocation in db.ResearchTargetAllocations.AsNoTracking()
                on assignment.DepartmentAllocationId equals allocation.Id
            join target in db.ResearchTargets.AsNoTracking()
                on allocation.ResearchTargetId equals target.Id
            join lecturer in db.Lecturers.AsNoTracking()
                on assignment.LecturerId equals lecturer.Id
            where target.Deadline.HasValue
                && target.Deadline.Value <= reminderLimit
                && target.Status == "DA_KHOA"
                && db.Products.Count(product =>
                    product.ResearchTargetId == target.Id
                    && product.SubmittedByLecturerId == lecturer.Id
                    && product.Status == "DA_DUYET"
                    && db.ResearchPlanItems.Any(item => item.Id == product.ResearchPlanItemId
                        && db.ProductTypes.Any(type => type.Id == item.ProductTypeId
                            && (type.Code != "BAI_BAO" || product.ArticleStatus == "DA_XUAT_BAN")))) < assignment.AllocatedArticleCount
            select new ReminderCandidate(
                lecturer.AccountId,
                "Chỉ tiêu NCKH (" + assignment.AllocatedArticleCount + " sản phẩm)",
                target.Deadline!.Value,
                "TARGET_ASSIGNMENT:" + assignment.Id,
                null,
                null);

        // PostgreSQL/EF Core không dịch được Concat sau projection có chuỗi định dạng.
        // Hai truy vấn nhỏ, rồi ghép trong bộ nhớ, giúp tác vụ chạy ổn định mỗi giờ.
        var planItems = await planItemsQuery.ToListAsync(cancellationToken);
        var assignedTargets = await assignedTargetsQuery.ToListAsync(cancellationToken);
        planItems.AddRange(assignedTargets);
        return planItems;
    }
}
