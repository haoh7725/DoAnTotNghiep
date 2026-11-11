using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.Products;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Infrastructure;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Authorize]
public sealed class ProductsController(
    ProductService productService,
    ApplicationDbContext db,
    ICurrentUser currentUser,
    WorkflowNotificationDispatcher workflowNotifications) : ControllerBase
{
    [HttpGet("api/products/pending-review")]
    [Authorize(Roles = "TRUONG_BO_MON,TRUONG_KHOA,PHONG_QLKH,QUAN_TRI")]
    public async Task<IActionResult> PendingReview(CancellationToken cancellationToken)
    {
        var accountId = currentUser.Id
            ?? throw new ForbiddenException("Không xác định được tài khoản.");

        var query =
            from product in db.Products.AsNoTracking()
            join lecturer in db.Lecturers
                on product.SubmittedByLecturerId equals lecturer.Id
            join department in db.Departments
                on lecturer.DepartmentId equals department.Id
            where
                (
                    product.Status == "NOP" ||
                    product.Status == "TRUONG_BO_MON_DUYET" ||
                    product.Status == "TRUONG_KHOA_DUYET" ||
                    product.Status == "PHONG_QLKH_DUYET"
                )
                && db.RoleAssignments.Any(role =>
                    role.AccountId == accountId &&
                    (
                        (role.Role == "QUAN_TRI" &&
                         role.Scope == "TOAN_TRUONG") ||

                        (role.Role == "TRUONG_BO_MON" &&
                         role.Scope == "BO_MON" &&
                         role.FacultyId == department.FacultyId &&
                         role.DepartmentId == department.Id &&
                         product.Status == "NOP" &&
                         !db.RoleAssignments.Any(ownerRole => ownerRole.AccountId == lecturer.AccountId &&
                             ownerRole.Role == Roles.DepartmentHead)) ||

                        (role.Role == "TRUONG_KHOA" &&
                         role.Scope == "KHOA" &&
                         role.FacultyId == department.FacultyId &&
                         ((product.Status == "NOP" &&
                           db.RoleAssignments.Any(ownerRole => ownerRole.AccountId == lecturer.AccountId &&
                               ownerRole.Role == Roles.DepartmentHead)) ||
                          (product.Status == "TRUONG_BO_MON_DUYET" &&
                           !db.RoleAssignments.Any(ownerRole => ownerRole.AccountId == lecturer.AccountId &&
                               ownerRole.Role == Roles.FacultyDean)))) ||

                        (role.Role == "PHONG_QLKH" &&
                         role.Scope == "TOAN_TRUONG" &&
                         ((product.Status == "TRUONG_BO_MON_DUYET" &&
                           db.RoleAssignments.Any(ownerRole => ownerRole.AccountId == lecturer.AccountId &&
                               ownerRole.Role == Roles.FacultyDean)) ||
                          product.Status == "TRUONG_KHOA_DUYET" ||
                          product.Status == "PHONG_QLKH_DUYET"))
                    ))
            orderby product.UpdatedAt
            select new
            {
                product.Id,
                product.Title,
                product.Status,
                LecturerName = lecturer.FullName,
                DepartmentName = department.Name
            };

        return Ok(await query.ToListAsync(cancellationToken));
    }
    [HttpGet("api/products/mine")]
    public async Task<ActionResult<IReadOnlyList<ProductSummaryResponse>>> Mine(CancellationToken cancellationToken) =>
        Ok(await productService.GetMineAsync(cancellationToken));

    [HttpGet("api/products/available-plan-items")]
    public async Task<IActionResult> AvailablePlanItems(CancellationToken cancellationToken)
    {
        var accountId = currentUser.Id ?? throw new ForbiddenException("Không xác định được tài khoản hiện tại.");
        var lecturerId = await db.Lecturers.Where(x => x.AccountId == accountId).Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Tài khoản chưa liên kết với giảng viên.");
        var items = await (from item in db.ResearchPlanItems.AsNoTracking()
                           join plan in db.ResearchPlans.AsNoTracking() on item.ResearchPlanId equals plan.Id
                           join year in db.AcademicYears.AsNoTracking() on plan.AcademicYearId equals year.Id
                           join type in db.ProductTypes.AsNoTracking() on item.ProductTypeId equals type.Id
                           where plan.LecturerId == lecturerId
                           orderby year.StartDate descending, item.Name
                           select new
                           {
                               item.Id,
                               item.Name,
                               ProductTypeName = type.Name,
                               AcademicYearCode = year.Code,
                               item.Status
                           }).ToListAsync(cancellationToken);
        return Ok(items);
    }

    // Danh sách gọn để giảng viên chọn đồng tác giả bằng tên thay vì phải biết ID nội bộ.
    [HttpGet("api/products/co-author-options")]
    public async Task<IActionResult> CoAuthorOptions(CancellationToken cancellationToken)
    {
        var accountId = currentUser.Id ?? throw new ForbiddenException("Không xác định được tài khoản hiện tại.");
        var currentLecturer = await db.Lecturers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.AccountId == accountId, cancellationToken)
            ?? throw new NotFoundException("Tài khoản chưa liên kết với giảng viên.");

        var facultyId = await db.Departments.AsNoTracking()
            .Where(x => x.Id == currentLecturer.DepartmentId)
            .Select(x => (long?)x.FacultyId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy đơn vị của giảng viên.");

        var lecturers = await (from lecturer in db.Lecturers.AsNoTracking()
                               join department in db.Departments.AsNoTracking()
                                   on lecturer.DepartmentId equals department.Id
                               where department.FacultyId == facultyId && lecturer.Id != currentLecturer.Id
                               orderby lecturer.FullName
                               select new
                               {
                                   lecturer.Id,
                                   lecturer.FullName,
                                   lecturer.Code,
                                   DepartmentName = department.Name
                               }).ToListAsync(cancellationToken);
        return Ok(lecturers);
    }

    [HttpGet("api/products/available-research-targets")]
    public async Task<IActionResult> AvailableResearchTargets(CancellationToken cancellationToken)
    {
        var accountId = currentUser.Id ?? throw new ForbiddenException("Không xác định được tài khoản hiện tại.");
        var lecturer = await db.Lecturers.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == accountId, cancellationToken)
            ?? throw new NotFoundException("Tài khoản chưa liên kết với giảng viên.");
        var targets = await (from allocation in db.ResearchTargetAllocations.AsNoTracking()
                             join personal in db.LecturerTargetAllocations.AsNoTracking() on allocation.Id equals personal.DepartmentAllocationId
                             join target in db.ResearchTargets.AsNoTracking() on allocation.ResearchTargetId equals target.Id
                             join year in db.AcademicYears.AsNoTracking() on target.AcademicYearId equals year.Id
                             join journal in db.JournalConferences.AsNoTracking() on target.JournalConferenceId equals journal.Id into journals
                             from journal in journals.DefaultIfEmpty()
                             where allocation.DepartmentId == lecturer.DepartmentId && personal.LecturerId == lecturer.Id
                                && target.Status == "DA_KHOA"
                             orderby year.StartDate descending, target.Id descending
                             select new { target.Id, target.AcademicYearId, AcademicYearCode = year.Code,
                                 AllocatedArticleCount = personal.AllocatedArticleCount, target.JournalConferenceId,
                                 JournalConferenceName = journal == null ? null : journal.Name,
                                 target.Deadline, target.Status }).ToListAsync(cancellationToken);
        return Ok(targets);
    }

    // GET /api/research-plan-items/{itemId}/products
    [HttpGet("api/research-plan-items/{itemId:long}/products")]
    public async Task<ActionResult<IReadOnlyList<ProductSummaryResponse>>> GetByPlanItem(
        long itemId, CancellationToken cancellationToken)
    {
        var result = await productService.GetByPlanItemIdAsync(itemId, cancellationToken);
        return Ok(result);
    }

    // POST /api/research-plan-items/{itemId}/products
    [HttpPost("api/research-plan-items/{itemId:long}/products")]
    public async Task<ActionResult<ProductResponse>> Create(
        long itemId,
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        request.ResearchPlanItemId = itemId;
        var result = await productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // GET /api/products/{id}
    [HttpGet("api/products/{id:long}")]
    public async Task<ActionResult<ProductResponse>> GetById(
        long id, CancellationToken cancellationToken)
    {
        var result = await productService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    // PUT /api/products/{id}
    [HttpPut("api/products/{id:long}")]
    public async Task<ActionResult<ProductResponse>> Update(
        long id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    // PATCH /api/products/{id}/article-progress
    // Chỉ cập nhật trạng thái xuất bản, không mở khóa toàn bộ nội dung sản phẩm.
    [HttpPatch("api/products/{id:long}/article-progress")]
    public async Task<ActionResult<ProductResponse>> UpdateArticleProgress(
        long id,
        [FromBody] UpdateArticleProgressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteInTransactionAsync(async () =>
        {
            var updated = await productService.UpdateArticleProgressAsync(id, request, cancellationToken);
            await QueueCurrentReviewerAsync(updated, "Tiến độ bài viết vừa được tác giả cập nhật", cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return updated;
        }, cancellationToken);
        return Ok(result);
    }

    public sealed record LinkTargetRequest(long ResearchTargetId);

    [HttpPost("api/products/{id:long}/link-target")]
    public async Task<ActionResult<ProductResponse>> LinkTarget(
        long id, LinkTargetRequest request, CancellationToken cancellationToken)
    {
        var result = await productService.LinkTargetAsync(id, request.ResearchTargetId, cancellationToken);
        return Ok(result);
    }

    // POST /api/products/{id}/submit
    [HttpPost("api/products/{id:long}/submit")]
    public async Task<ActionResult<ProductResponse>> Submit(
        long id, CancellationToken cancellationToken)
    {
        var result = await ExecuteInTransactionAsync(async () =>
        {
            var submitted = await productService.SubmitAsync(id, cancellationToken);
            var unit = await ProductUnitAsync(submitted.SubmittedByLecturerId, cancellationToken);
            var ownerIsDepartmentHead = await OwnerHasRoleAsync(submitted.SubmittedByLecturerId, Roles.DepartmentHead, cancellationToken);
            await workflowNotifications.QueueForRoleAsync(
                ownerIsDepartmentHead ? Roles.FacultyDean : Roles.DepartmentHead,
                $"Sản phẩm '{submitted.Title}' vừa được giảng viên gửi duyệt. Vui lòng kiểm tra và xét duyệt.",
                $"PRODUCT_SUBMITTED:{submitted.Id}",
                $"product-reviews:{submitted.Id}",
                unit.FacultyId,
                ownerIsDepartmentHead ? null : unit.DepartmentId,
                submitted.Id,
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return submitted;
        }, cancellationToken);
        return Ok(result);
    }

    // POST /api/products/{id}/review
    [HttpPost("api/products/{id:long}/review")]
    public async Task<ActionResult<ProductResponse>> Review(
        long id,
        [FromBody] ReviewProductRequest request,
        CancellationToken cancellationToken)
    {
        var before = await db.Products.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Status, x.SubmittedByLecturerId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        var result = await ExecuteInTransactionAsync(async () =>
        {
            var reviewed = await productService.ReviewAsync(id, request, cancellationToken);
            if (request.Action.Trim().Equals("APPROVE", StringComparison.OrdinalIgnoreCase))
            {
                var unit = await ProductUnitAsync(before.SubmittedByLecturerId, cancellationToken);
                if (reviewed.Status == ProductStatuses.DepartmentHeadApproved)
                {
                    var ownerIsDean = await OwnerHasRoleAsync(before.SubmittedByLecturerId, Roles.FacultyDean, cancellationToken);
                    await workflowNotifications.QueueForRoleAsync(ownerIsDean ? Roles.ResearchOffice : Roles.FacultyDean,
                        ownerIsDean
                            ? $"Sản phẩm '{reviewed.Title}' của Trưởng khoa đã qua bước bộ môn và đang chờ Phòng QLKH thẩm định."
                            : $"Sản phẩm '{reviewed.Title}' đã được Trưởng bộ môn duyệt và đang chờ Trưởng khoa xét duyệt.",
                        ownerIsDean ? $"PRODUCT_WAIT_OFFICE:{reviewed.Id}" : $"PRODUCT_WAIT_DEAN:{reviewed.Id}",
                        $"product-reviews:{reviewed.Id}", unit.FacultyId,
                        ownerIsDean ? null : unit.DepartmentId, reviewed.Id, cancellationToken);
                }
                else if (reviewed.Status == ProductStatuses.FacultyDeanApproved)
                    await workflowNotifications.QueueForRoleAsync(Roles.ResearchOffice,
                        $"Sản phẩm '{reviewed.Title}' đã được Trưởng khoa duyệt và đang chờ Phòng QLKH thẩm định, công nhận.",
                        $"PRODUCT_WAIT_OFFICE:{reviewed.Id}", $"product-reviews:{reviewed.Id}",
                        unit.FacultyId, unit.DepartmentId, reviewed.Id, cancellationToken);
                else if (reviewed.Status == ProductStatuses.Approved)
                {
                    var authorAccountIds = await db.Lecturers.AsNoTracking()
                        .Where(x => x.Id == before.SubmittedByLecturerId ||
                            db.ProductCoAuthors.Any(a => a.ProductId == reviewed.Id && a.LecturerId == x.Id))
                        .Select(x => x.AccountId).Distinct().ToListAsync(cancellationToken);
                    foreach (var accountId in authorAccountIds)
                        await workflowNotifications.QueueForAccountAsync(accountId,
                            $"Sản phẩm '{reviewed.Title}' đã được Phòng QLKH công nhận hoàn tất.",
                            $"PRODUCT_APPROVED:{reviewed.Id}", $"products:{reviewed.Id}", reviewed.Id, cancellationToken);
                }
                await db.SaveChangesAsync(cancellationToken);
            }
            return reviewed;
        }, cancellationToken);
        return Ok(result);
    }

    // GET /api/products/{id}/review-history
    [HttpGet("api/products/{id:long}/snapshots")]
    public async Task<IActionResult> Snapshots(long id, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null) return NotFound();
        var owner = await db.Lecturers.AsNoTracking()
            .SingleAsync(l => l.Id == product.SubmittedByLecturerId, cancellationToken);
        var department = await db.Departments.AsNoTracking()
            .SingleAsync(d => d.Id == owner.DepartmentId, cancellationToken);
        var isCoAuthor = await db.ProductCoAuthors.AnyAsync(a => a.ProductId == id &&
            db.Lecturers.Any(l => l.Id == a.LecturerId && l.AccountId == currentUser.Id), cancellationToken);
        if (owner.AccountId != currentUser.Id && !isCoAuthor &&
            !currentUser.CanAccess(department.FacultyId, department.Id)) return Forbid();
        return Ok(await db.ProductSnapshots.AsNoTracking().Where(s => s.ProductId == id)
            .OrderBy(s => s.Version).ToListAsync(cancellationToken));
    }

    [HttpGet("api/products/{id:long}/review-history")]
    public async Task<ActionResult<IReadOnlyList<ReviewHistoryResponse>>> GetReviewHistory(
        long id, CancellationToken cancellationToken)
    {
        var result = await productService.GetReviewHistoryAsync(id, cancellationToken);
        return Ok(result);
    }

    private async Task<(long FacultyId, long DepartmentId)> ProductUnitAsync(long lecturerId, CancellationToken ct)
    {
        var unit = await (from lecturer in db.Lecturers.AsNoTracking()
                          join department in db.Departments.AsNoTracking() on lecturer.DepartmentId equals department.Id
                          where lecturer.Id == lecturerId
                          select new { department.FacultyId, DepartmentId = department.Id })
            .SingleAsync(ct);
        return (unit.FacultyId, unit.DepartmentId);
    }

    private Task<bool> OwnerHasRoleAsync(long lecturerId, string role, CancellationToken ct) =>
        db.Lecturers.AsNoTracking().AnyAsync(lecturer => lecturer.Id == lecturerId &&
            db.RoleAssignments.Any(assignment => assignment.AccountId == lecturer.AccountId && assignment.Role == role), ct);

    private async Task QueueCurrentReviewerAsync(ProductResponse product, string action, CancellationToken ct)
    {
        var unit = await ProductUnitAsync(product.SubmittedByLecturerId, ct);
        string? role = null;
        long? departmentId = unit.DepartmentId;
        if (product.Status == ProductStatuses.Submitted)
        {
            var ownerIsDepartmentHead = await OwnerHasRoleAsync(product.SubmittedByLecturerId, Roles.DepartmentHead, ct);
            role = ownerIsDepartmentHead ? Roles.FacultyDean : Roles.DepartmentHead;
            if (ownerIsDepartmentHead) departmentId = null;
        }
        else if (product.Status == ProductStatuses.DepartmentHeadApproved)
        {
            var ownerIsDean = await OwnerHasRoleAsync(product.SubmittedByLecturerId, Roles.FacultyDean, ct);
            role = ownerIsDean ? Roles.ResearchOffice : Roles.FacultyDean;
            departmentId = null;
        }
        else if (product.Status is ProductStatuses.FacultyDeanApproved or ProductStatuses.ResearchOfficeApproved)
        {
            role = Roles.ResearchOffice;
            departmentId = null;
        }
        if (role is null) return;
        await workflowNotifications.QueueForRoleAsync(role,
            $"{action}: '{product.Title}'.", $"ARTICLE_PROGRESS:{product.Id}:{product.ArticleStatus}:{product.UpdatedAt.UtcTicks}",
            $"product-reviews:{product.Id}", unit.FacultyId, departmentId, product.Id, ct);
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var result = await action();
            await transaction.CommitAsync(ct);
            return result;
        });
    }
}
