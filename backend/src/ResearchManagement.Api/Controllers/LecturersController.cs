using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Lecturers;
using ResearchManagement.Application.Lecturers.Models;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ResearchManagement.Api.Controllers;

/// <summary>
/// Hồ sơ giảng viên và lý lịch khoa học. Controller chỉ chặn theo vai trò; phạm vi khoa/bộ môn
/// và quyền chính chủ do LecturerService, ScientificProfileService kiểm tra.
/// </summary>
[ApiController]
[Route("api/lecturers")]
[Authorize]
public sealed class LecturersController(
    LecturerService lecturers,
    ScientificProfileService profiles,
    ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<LecturerResponse>>> Search(
        [FromQuery] LecturerSearchQuery query, CancellationToken cancellationToken) =>
        Ok(await lecturers.SearchAsync(query, cancellationToken));

    [HttpGet("account-options")]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<IReadOnlyList<LecturerAccountOptionResponse>>> GetAccountOptions(
        CancellationToken cancellationToken) =>
        Ok(await lecturers.GetAvailableAccountOptionsAsync(cancellationToken));

    // Các route "me" phải đứng cùng nhóm với {id:long}; ràng buộc :long nên không xung đột.
    [HttpGet("me")]
    public async Task<ActionResult<LecturerResponse>> GetMine(CancellationToken cancellationToken) =>
        Ok(await lecturers.GetMineAsync(cancellationToken));

    [HttpPut("me")]
    public async Task<ActionResult<LecturerResponse>> UpdateMine(
        [FromBody] UpdateMyLecturerRequest request, CancellationToken cancellationToken) =>
        Ok(await lecturers.UpdateMineAsync(request, cancellationToken));

    [HttpGet("me/scientific-profile")]
    public async Task<ActionResult<ScientificProfileResponse>> GetMyProfile(CancellationToken cancellationToken) =>
        Ok(await profiles.GetMineAsync(cancellationToken));

    [HttpPut("me/scientific-profile")]
    public async Task<ActionResult<ScientificProfileResponse>> UpsertMyProfile(
        [FromBody] ScientificProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await profiles.UpsertMineAsync(request, cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<LecturerResponse>> GetById(long id, CancellationToken cancellationToken) =>
        Ok(await lecturers.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<LecturerResponse>> Create(
        [FromBody] CreateLecturerRequest request, CancellationToken cancellationToken)
    {
        var result = await lecturers.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<LecturerResponse>> Update(
        long id, [FromBody] UpdateLecturerRequest request, CancellationToken cancellationToken) =>
        Ok(await lecturers.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:long}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await lecturers.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/scientific-profile")]
    public async Task<ActionResult<ScientificProfileResponse>> GetProfile(
        long id, CancellationToken cancellationToken) =>
        Ok(await profiles.GetAsync(id, cancellationToken));

    [HttpGet("{id:long}/approved-research")]
    public async Task<IActionResult> GetApprovedResearch(long id, CancellationToken cancellationToken)
    {
        // Dùng cùng kiểm tra phạm vi với màn hồ sơ giảng viên.
        _ = await lecturers.GetByIdAsync(id, cancellationToken);

        var products = await (from product in db.Products.AsNoTracking()
                              join planItem in db.ResearchPlanItems.AsNoTracking()
                                  on product.ResearchPlanItemId equals planItem.Id
                              join plan in db.ResearchPlans.AsNoTracking()
                                  on planItem.ResearchPlanId equals plan.Id
                              join productType in db.ProductTypes.AsNoTracking()
                                  on planItem.ProductTypeId equals productType.Id
                              from academicYear in db.AcademicYears.AsNoTracking()
                              let coAuthorRole = db.ProductCoAuthors
                                  .Where(author => author.ProductId == product.Id && author.LecturerId == id)
                                  .Select(author => author.AuthorRole)
                                  .FirstOrDefault()
                              where (product.PublishedDate.HasValue
                                        ? product.PublishedDate >= academicYear.StartDate && product.PublishedDate <= academicYear.EndDate
                                        : academicYear.Id == plan.AcademicYearId) && product.Status == ProductStatuses.Approved
                                    && (product.SubmittedByLecturerId == id || coAuthorRole != null)
                              orderby academicYear.StartDate descending, product.PublishedDate descending, product.Title
                              select new
                              {
                                  product.Id,
                                  product.Title,
                                  ProductType = productType.Name,
                                  ProductTypeCode = productType.Code,
                                  AcademicYear = product.PublishedDate.HasValue ? academicYear.Code : "Chưa xác định",
                                  StartYear = academicYear.StartDate.Year,
                                  EndYear = academicYear.EndDate.Year,
                                  product.PublishedDate,
                                  product.PublicationInfo,
                                  product.ProjectLevel,
                                  AuthorRole = product.SubmittedByLecturerId == id ? "TAC_GIA_CHINH" : coAuthorRole
                              }).ToListAsync(cancellationToken);

        return Ok(products);
    }

    [HttpPut("{id:long}/scientific-profile")]
    public async Task<ActionResult<ScientificProfileResponse>> UpsertProfile(
        long id, [FromBody] ScientificProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await profiles.UpsertAsync(id, request, cancellationToken));
}
