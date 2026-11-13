using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/research-progress")]
[Authorize]
public sealed class ResearchProgressController(
    ApplicationDbContext db,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("targets/{targetId:long}")]
    [Authorize(Policy = ResearchManagement.Api.Authorization.Policies.Management)]
    public async Task<IActionResult> TargetProgress(long targetId, CancellationToken ct)
    {
        var target = await db.ResearchTargets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == targetId, ct)
            ?? throw new NotFoundException("Không tìm thấy chỉ tiêu NCKH.");
        var canViewWholeFaculty = currentUser.CanAccess(target.FacultyId, null);
        if (!canViewWholeFaculty && target.Status != "DA_KHOA")
            throw new ForbiddenException("Tiến độ bộ môn chỉ được xem sau khi Trưởng khoa khóa phân bổ.");
        var allocations = await (from allocation in db.ResearchTargetAllocations.AsNoTracking()
                                 join department in db.Departments.AsNoTracking() on allocation.DepartmentId equals department.Id
                                 where allocation.ResearchTargetId == targetId
                                 orderby department.Name
                                 select new { allocation.DepartmentId, DepartmentName = department.Name, allocation.AllocatedArticleCount }).ToListAsync(ct);
        if (!canViewWholeFaculty)
            allocations = allocations.Where(x => currentUser.CanAccess(target.FacultyId, x.DepartmentId)).ToList();
        if (!canViewWholeFaculty && allocations.Count == 0)
            throw new ForbiddenException("Không có quyền xem tiến độ chỉ tiêu này.");
        var approved = await (from allocation in db.ResearchTargetAllocations.AsNoTracking()
                              join personal in db.LecturerTargetAllocations.AsNoTracking() on allocation.Id equals personal.DepartmentAllocationId
                              from product in db.Products.AsNoTracking()
                              join item in db.ResearchPlanItems.AsNoTracking() on product.ResearchPlanItemId equals item.Id
                              join type in db.ProductTypes.AsNoTracking() on item.ProductTypeId equals type.Id
                              where allocation.ResearchTargetId == targetId && product.ResearchTargetId == targetId && type.Id == target.ProductTypeId
                                  && product.SubmittedByLecturerId == personal.LecturerId
                                  && product.Status == "DA_DUYET" && (type.Code != "BAI_BAO" || product.ArticleStatus == "DA_XUAT_BAN")
                              group product by allocation.DepartmentId into grouped
                              select new { DepartmentId = grouped.Key, Count = grouped.Select(x => x.Id).Distinct().Count() })
            .ToDictionaryAsync(x => x.DepartmentId, x => x.Count, ct);
        var units = allocations.Select(x => new { x.DepartmentId, x.DepartmentName,
            AllocatedArticles = x.AllocatedArticleCount, ApprovedArticles = approved.GetValueOrDefault(x.DepartmentId),
            CompletionPercent = x.AllocatedArticleCount == 0 ? 0m : Math.Round(approved.GetValueOrDefault(x.DepartmentId) * 100m / x.AllocatedArticleCount, 2),
            IsCompleted = approved.GetValueOrDefault(x.DepartmentId) >= x.AllocatedArticleCount }).ToList();
        var visibleDepartmentIds = allocations.Select(x => x.DepartmentId).ToList();
        // Số lượng chỉ tiêu tính cho giảng viên phụ trách bài; đồng tác giả được quy đổi đóng góp riêng.
        var totalApproved = await (from product in db.Products.AsNoTracking()
                                   join item in db.ResearchPlanItems.AsNoTracking() on product.ResearchPlanItemId equals item.Id
                                   join type in db.ProductTypes.AsNoTracking() on item.ProductTypeId equals type.Id
                                   where product.ResearchTargetId == targetId && type.Id == target.ProductTypeId &&
                                       product.Status == "DA_DUYET" && (type.Code != "BAI_BAO" || product.ArticleStatus == "DA_XUAT_BAN") &&
                                       db.LecturerTargetAllocations.Any(personal =>
                                           personal.LecturerId == product.SubmittedByLecturerId &&
                                           db.ResearchTargetAllocations.Any(allocation =>
                                               allocation.Id == personal.DepartmentAllocationId &&
                                               allocation.ResearchTargetId == targetId &&
                                               visibleDepartmentIds.Contains(allocation.DepartmentId)))
                                   select product.Id).Distinct().CountAsync(ct);
        var visibleTarget = canViewWholeFaculty ? target.AssignedArticleCount : units.Sum(x => x.AllocatedArticles);
        return Ok(new { target.Id, AssignedArticleCount = visibleTarget, target.Status, ApprovedArticles = totalApproved,
            CompletionPercent = visibleTarget == 0 ? 0m : Math.Round(totalApproved * 100m / visibleTarget, 2),
            IsCompleted = totalApproved >= visibleTarget, Units = units });
    }

    [HttpGet("targets/my-department")]
    public async Task<IActionResult> MyDepartmentTargets(
    [FromQuery] long academicYearId,
    CancellationToken ct)
    {
        if (academicYearId <= 0)
            throw new BusinessRuleException("Vui lòng chọn năm học.");

        var accountId = currentUser.Id
            ?? throw new ForbiddenException(
                "Không xác định được tài khoản.");

        var lecturer = await db.Lecturers.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.AccountId == accountId, ct)
            ?? throw new NotFoundException(
                "Tài khoản chưa liên kết với giảng viên.");

        var department = await db.Departments.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == lecturer.DepartmentId, ct)
            ?? throw new NotFoundException(
                "Không tìm thấy bộ môn của giảng viên.");

        var year = await db.AcademicYears.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == academicYearId, ct)
            ?? throw new NotFoundException(
                "Không tìm thấy năm học.");

        // Tổng chỉ tiêu đã được phân bổ cho bộ môn trong đúng năm học đang xem.
        var allocations = await (
            from allocation in db.ResearchTargetAllocations.AsNoTracking()
            join target in db.ResearchTargets.AsNoTracking()
                on allocation.ResearchTargetId equals target.Id
            where allocation.DepartmentId == department.Id
                && allocation.FacultyId == department.FacultyId
                && target.FacultyId == department.FacultyId
                && target.AcademicYearId == academicYearId
                && target.Status == "DA_KHOA"
            select allocation.AllocatedArticleCount
        ).ToListAsync(ct);

        var hasAllocation = allocations.Count > 0;

        int? allocatedArticles = hasAllocation
            ? allocations.Sum()
            : null;

        // Chỉ đếm sản phẩm đã được duyệt hoàn tất. Bài báo còn yêu cầu trạng thái xuất bản;
        // các loại sản phẩm khác được tính ngay khi được công nhận.
        // Bộ môn được xác định từ giảng viên đứng tên gửi.
        var approvedArticleIds =
            from product in db.Products.AsNoTracking()
            join item in db.ResearchPlanItems.AsNoTracking()
                on product.ResearchPlanItemId equals item.Id
            join plan in db.ResearchPlans.AsNoTracking()
                on item.ResearchPlanId equals plan.Id
            join productType in db.ProductTypes.AsNoTracking()
                on item.ProductTypeId equals productType.Id
            where plan.AcademicYearId == academicYearId
                && product.Status == "DA_DUYET"
                && (productType.Code != "BAI_BAO" || product.ArticleStatus == "DA_XUAT_BAN")
                && product.ResearchTargetId.HasValue
                && db.LecturerTargetAllocations.Any(personal =>
                    personal.LecturerId == product.SubmittedByLecturerId &&
                    db.ResearchTargetAllocations.Any(a => a.Id == personal.DepartmentAllocationId &&
                        a.ResearchTargetId == product.ResearchTargetId && a.DepartmentId == department.Id &&
                        a.FacultyId == department.FacultyId))
            select product.Id;

        var approvedProducts = await approvedArticleIds
            .Distinct()
            .CountAsync(ct);

        decimal? completionPercent = null;

        if (allocatedArticles.HasValue &&
            allocatedArticles.Value > 0)
        {
            completionPercent = Math.Round(
                approvedProducts * 100m / allocatedArticles.Value,
                2);
        }

        return Ok(new
        {
            AcademicYearId = year.Id,
            AcademicYearCode = year.Code,
            DepartmentId = department.Id,
            DepartmentName = department.Name,
            HasAllocation = hasAllocation,
            AllocatedArticles = allocatedArticles,
            ApprovedArticles = approvedProducts,
            CompletionPercent = completionPercent,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
    [HttpGet("targets/me")]
    public async Task<IActionResult> MyProductTargets(
    [FromQuery] long academicYearId,
    CancellationToken ct)
    {
        if (academicYearId <= 0)
            throw new BusinessRuleException("Vui lòng chọn năm học.");

        var accountId = currentUser.Id
            ?? throw new ForbiddenException(
                "Không xác định được tài khoản.");

        var lecturer = await db.Lecturers.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.AccountId == accountId, ct)
            ?? throw new NotFoundException(
                "Tài khoản chưa liên kết với giảng viên.");

        var year = await db.AcademicYears.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == academicYearId, ct)
            ?? throw new NotFoundException(
                "Không tìm thấy năm học.");

        var plan = await db.ResearchPlans.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.LecturerId == lecturer.Id &&
                     x.AcademicYearId == academicYearId,
                ct);

        int? committedProducts = plan?.CommittedProductCount;
        int? approvedProducts = null;

        if (plan != null)
        {
            // Tiến độ kế hoạch cá nhân chỉ tính sản phẩm thuộc chính kế hoạch đó.
            // Đồng tác giả ở kế hoạch của người khác vẫn được tính khi đánh giá cuối năm,
            // nhưng không làm tăng tỷ lệ hoàn thành cam kết cá nhân tại đây.
            var query =
                from product in db.Products.AsNoTracking()
                join item in db.ResearchPlanItems.AsNoTracking()
                    on product.ResearchPlanItemId equals item.Id
                join productPlan in db.ResearchPlans.AsNoTracking()
                    on item.ResearchPlanId equals productPlan.Id
                where productPlan.Id == plan.Id
                    && product.SubmittedByLecturerId == lecturer.Id
                    && product.Status == "DA_DUYET"
                select product.Id;

            approvedProducts = await query
                .Distinct()
                .CountAsync(ct);
        }

        var hasRegisteredPlan = plan?.Status == "DA_DANG_KY";
        decimal? completionPercent = null;

        if (hasRegisteredPlan &&
            committedProducts.HasValue &&
            committedProducts.Value > 0 &&
            approvedProducts.HasValue)
        {
            completionPercent = Math.Round(
                approvedProducts.Value * 100m /
                committedProducts.Value,
                2);
        }

        return Ok(new
        {
            AcademicYearId = year.Id,
            AcademicYearCode = year.Code,
            LecturerName = lecturer.FullName,
            HasPlan = plan != null,
            HasRegisteredPlan = hasRegisteredPlan,
            PlanStatus = plan?.Status,
            CommittedProducts = committedProducts,
            ApprovedProducts = approvedProducts,
            CompletionPercent = completionPercent,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
    [HttpGet("me")]
    public async Task<IActionResult> Mine(
        [FromQuery] long academicYearId,
        CancellationToken ct)
    {
        if (academicYearId <= 0)
            throw new BusinessRuleException("Vui lòng chọn năm học.");

        var accountId = currentUser.Id
            ?? throw new ForbiddenException(
                "Không xác định được tài khoản.");

        // Tự xác định giảng viên từ tài khoản đăng nhập.
        // Không nhận lecturerId từ người gọi.
        var lecturer = await db.Lecturers.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.AccountId == accountId, ct)
            ?? throw new NotFoundException(
                "Tài khoản chưa liên kết với giảng viên.");

        var year = await db.AcademicYears.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == academicYearId, ct)
            ?? throw new NotFoundException(
                "Không tìm thấy năm học.");

        var norm = await db.ResearchNorms.AsNoTracking()
            .Where(x =>
                x.LecturerId == lecturer.Id &&
                x.AcademicYearId == academicYearId)
            .Select(x => new { x.RequiredHours, x.RequiredPoints })
            .SingleOrDefaultAsync(ct);

        // Cùng nguyên tắc với báo cáo Web:
        // lấy lần đánh giá đã chốt mới nhất.
        var evaluation = await db.Evaluations.AsNoTracking()
            .Where(x =>
                x.LecturerId == lecturer.Id &&
                x.AcademicYearId == academicYearId &&
                x.Status == "CHOT")
            .OrderByDescending(x => x.Attempt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        decimal? confirmedHours = null;
        decimal? confirmedPoints = null;
        int? confirmedProductCount = null;

        if (evaluation != null)
        {
            var details = db.EvaluationConversionDetails
                .AsNoTracking()
                .Where(x => x.EvaluationId == evaluation.Id);

            confirmedHours = await details
                .Where(x => x.Unit == "GIO")
                .SumAsync(x => x.ConvertedValue, ct);

            confirmedPoints = await details
                .Where(x => x.Unit == "DIEM")
                .SumAsync(x => x.ConvertedValue, ct);

            // Một sản phẩm có thể có nhiều dòng quy đổi.
            confirmedProductCount = await details
                .Select(x => x.ProductId)
                .Distinct()
                .CountAsync(ct);
        }

        decimal? completionPercent = null;

        var evaluationUnit = evaluation?.EvaluationUnit ?? "GIO";
        var requiredValue = evaluationUnit == "DIEM" ? norm?.RequiredPoints : norm?.RequiredHours;
        var confirmedValue = evaluationUnit == "DIEM" ? confirmedPoints : confirmedHours;
        if (evaluationUnit != "LEGACY" && requiredValue > 0 && confirmedValue.HasValue)
            completionPercent = Math.Round(confirmedValue.Value * 100m / requiredValue.Value, 2);
        return Ok(new
        {
            AcademicYearId = year.Id,
            AcademicYearCode = year.Code,
            LecturerName = lecturer.FullName,
            RequiredHours = norm?.RequiredHours,
            RequiredPoints = norm?.RequiredPoints,
            EvaluationUnit = evaluationUnit,
            HasFinalizedEvaluation = evaluation != null,
            ConfirmedHours = confirmedHours,
            ConfirmedPoints = confirmedPoints,
            ConfirmedProductCount = confirmedProductCount,
            CompletionPercent = completionPercent,
            EvaluatedAt = evaluation?.EvaluatedAt
        });
    }
}
