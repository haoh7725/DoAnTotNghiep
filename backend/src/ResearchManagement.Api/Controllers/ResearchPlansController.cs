using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.ResearchPlans;
using ResearchManagement.Application.ResearchPlans.Models;
using ResearchManagement.Application.Common;
using ResearchManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Infrastructure;
using ResearchManagement.Api.Authorization;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/research-plans")]
[Authorize]
public sealed class ResearchPlansController(
    ResearchPlanService researchPlanService,
    ApplicationDbContext db,
    WorkflowNotificationDispatcher workflowNotifications) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ResearchPlanResponse>> GetByLecturerAndAcademicYear(
        [FromQuery] long lecturerId,
        [FromQuery] long academicYearId,
        CancellationToken cancellationToken)
    {
        if (lecturerId <= 0 || academicYearId <= 0)
        {
            return BadRequest(
                "Giảng viên và năm học phải hợp lệ.");
        }

        var result = await researchPlanService.GetByLecturerAndAcademicYearAsync(
            lecturerId,
            academicYearId,
            cancellationToken);

        return result is null ? NoContent() : Ok(result);
    }


    [HttpGet("{id:long}")]
    public async Task<ActionResult<ResearchPlanResponse>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await researchPlanService.GetByIdAsync(
            id,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ResearchPlanResponse>> Create(
        [FromBody] CreateResearchPlanRequest request,
        CancellationToken cancellationToken)
    {
        var year = await db.AcademicYears.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.AcademicYearId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy năm học.");
        if (request.PlanDate < year.StartDate || request.PlanDate > year.EndDate)
            throw new BusinessRuleException("Ngày lập kế hoạch phải nằm trong năm học.");
        if (request.Deadline.HasValue &&
            (request.Deadline.Value < year.StartDate || request.Deadline.Value > year.EndDate))
            throw new BusinessRuleException("Hạn kế hoạch phải nằm trong năm học.");

        var result = await researchPlanService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ResearchPlanResponse>> Update(
        long id,
        [FromBody] UpdateResearchPlanRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await db.ResearchPlans.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy kế hoạch NCKH.");
        var year = await db.AcademicYears.AsNoTracking()
            .SingleAsync(x => x.Id == plan.AcademicYearId, cancellationToken);
        if (request.Deadline.HasValue &&
            (request.Deadline.Value < year.StartDate || request.Deadline.Value > year.EndDate))
            throw new BusinessRuleException("Hạn kế hoạch phải nằm trong năm học.");

        var result = await researchPlanService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:long}/submit")]
    public async Task<ActionResult<ResearchPlanResponse>> Submit(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await researchPlanService.SubmitAsync(
            id,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:long}/reopen")]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<ResearchPlanResponse>> Reopen(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await researchPlanService.ReopenAsync(id, cancellationToken);
        var ownerAccountId = await db.Lecturers.AsNoTracking()
            .Where(x => x.Id == result.LecturerId)
            .Select(x => x.AccountId)
            .SingleAsync(cancellationToken);
        await workflowNotifications.QueueForAccountAsync(
            ownerAccountId,
            "Phòng QLKH đã mở lại kế hoạch NCKH để bạn điều chỉnh. Vui lòng cập nhật và đăng ký lại.",
            $"RESEARCH_PLAN_REOPENED:{result.Id}:{Guid.NewGuid():N}",
            "research-plans",
            null,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(result);
    }
}
