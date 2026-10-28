using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.Products;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Authorize]
public sealed class ProductsController(ProductService productService, ApplicationDbContext db, ICurrentUser currentUser) : ControllerBase
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
                         product.Status == "NOP") ||

                        (role.Role == "TRUONG_KHOA" &&
                         role.Scope == "KHOA" &&
                         role.FacultyId == department.FacultyId &&
                         product.Status == "TRUONG_BO_MON_DUYET") ||

                        (role.Role == "PHONG_QLKH" &&
                         role.Scope == "TOAN_TRUONG" &&
                         (product.Status == "TRUONG_KHOA_DUYET" ||
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

    // POST /api/products/{id}/submit
    [HttpPost("api/products/{id:long}/submit")]
    public async Task<ActionResult<ProductResponse>> Submit(
        long id, CancellationToken cancellationToken)
    {
        var result = await productService.SubmitAsync(id, cancellationToken);
        return Ok(result);
    }

    // POST /api/products/{id}/review
    [HttpPost("api/products/{id:long}/review")]
    public async Task<ActionResult<ProductResponse>> Review(
        long id,
        [FromBody] ReviewProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.ReviewAsync(id, request, cancellationToken);
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
}
