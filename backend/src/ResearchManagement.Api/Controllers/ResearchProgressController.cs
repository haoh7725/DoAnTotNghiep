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

        // Chỉ tiêu bài báo đã được phân bổ cho bộ môn
        // trong đúng năm học đang xem.
        var allocations = await (
            from allocation in db.ResearchTargetAllocations.AsNoTracking()
            join target in db.ResearchTargets.AsNoTracking()
                on allocation.ResearchTargetId equals target.Id
            where allocation.DepartmentId == department.Id
                && allocation.FacultyId == department.FacultyId
                && target.FacultyId == department.FacultyId
                && target.AcademicYearId == academicYearId
            select allocation.AllocatedArticleCount
        ).ToListAsync(ct);

        var hasAllocation = allocations.Count > 0;

        int? allocatedArticles = hasAllocation
            ? allocations.Sum()
            : null;

        // Chỉ đếm bài báo đã duyệt hoàn tất.
        // Bộ môn được xác định từ giảng viên đứng tên gửi.
        var approvedArticleIds =
            from product in db.Products.AsNoTracking()
            join author in db.Lecturers.AsNoTracking()
                on product.SubmittedByLecturerId equals author.Id
            join item in db.ResearchPlanItems.AsNoTracking()
                on product.ResearchPlanItemId equals item.Id
            join plan in db.ResearchPlans.AsNoTracking()
                on item.ResearchPlanId equals plan.Id
            join productType in db.ProductTypes.AsNoTracking()
                on item.ProductTypeId equals productType.Id
            where author.DepartmentId == department.Id
                && plan.LecturerId == author.Id
                && plan.AcademicYearId == academicYearId
                && productType.Code == "BAI_BAO"
                && product.Status == "DA_DUYET"
            select product.Id;

        var approvedArticles = await approvedArticleIds
            .Distinct()
            .CountAsync(ct);

        decimal? completionPercent = null;

        if (allocatedArticles.HasValue &&
            allocatedArticles.Value > 0)
        {
            completionPercent = Math.Round(
                approvedArticles * 100m / allocatedArticles.Value,
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
            ApprovedArticles = approvedArticles,
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
            // Chỉ tính sản phẩm của giảng viên này,
            // gắn với nội dung thuộc đúng kế hoạch đang xem.
            var query =
                from product in db.Products.AsNoTracking()
                join item in db.ResearchPlanItems.AsNoTracking()
                    on product.ResearchPlanItemId equals item.Id
                where item.ResearchPlanId == plan.Id
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

        var requiredHours = await db.ResearchNorms.AsNoTracking()
            .Where(x =>
                x.LecturerId == lecturer.Id &&
                x.AcademicYearId == academicYearId)
            .Select(x => (decimal?)x.RequiredHours)
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

        if (requiredHours.HasValue &&
            requiredHours.Value > 0 &&
            confirmedHours.HasValue)
        {
            completionPercent = Math.Round(
                confirmedHours.Value * 100m /
                requiredHours.Value,
                2);
        }

        return Ok(new
        {
            AcademicYearId = year.Id,
            AcademicYearCode = year.Code,
            LecturerName = lecturer.FullName,
            RequiredHours = requiredHours,
            HasFinalizedEvaluation = evaluation != null,
            ConfirmedHours = confirmedHours,
            ConfirmedPoints = confirmedPoints,
            ConfirmedProductCount = confirmedProductCount,
            CompletionPercent = completionPercent,
            EvaluatedAt = evaluation?.EvaluatedAt
        });
    }
}
