using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.ResearchPlans;
using ResearchManagement.Application.ResearchPlans.Models;
using ResearchManagement.Application.Common;
using ResearchManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Infrastructure;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/research-plans")]
[Authorize]
public sealed class ResearchPlansController(
    ResearchPlanService researchPlanService,
    ApplicationDbContext db,
    ICurrentUser currentUser,
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
        long id, [FromBody] ReopenPlanRequest request,
        CancellationToken cancellationToken)
    {
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)
            throw new BusinessRuleException("Phải nhập lý do mở lại kế hoạch (tối đa 1000 ký tự).");
        var strategy = db.Database.CreateExecutionStrategy();
        var result = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var previousStatus = await db.ResearchPlans.AsNoTracking()
                .Where(x => x.Id == id).Select(x => x.Status).SingleOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy kế hoạch NCKH.");
            var reopened = await researchPlanService.ReopenAsync(id, cancellationToken);
            db.ResearchPlanHistories.Add(new ResearchPlanHistory(
                reopened.Id,
                currentUser.Id ?? throw new ForbiddenException("Không xác định được tài khoản."),
                previousStatus,
                reopened.Status,
                reason));
            var ownerAccountId = await db.Lecturers.AsNoTracking()
                .Where(x => x.Id == reopened.LecturerId)
                .Select(x => x.AccountId)
                .SingleAsync(cancellationToken);
            await workflowNotifications.QueueForAccountAsync(
                ownerAccountId,
                $"Phòng QLKH đã mở lại kế hoạch NCKH để bạn điều chỉnh. Lý do: {reason}",
                $"RESEARCH_PLAN_REOPENED:{reopened.Id}:{Guid.NewGuid():N}",
                "research-plans",
                null,
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return reopened;
        });
        return Ok(result);
    }

    [HttpGet("{id:long}/history")]
    public async Task<IActionResult> History(long id, CancellationToken cancellationToken)
    {
        var access = await (from plan in db.ResearchPlans.AsNoTracking()
                            join lecturer in db.Lecturers.AsNoTracking() on plan.LecturerId equals lecturer.Id
                            join department in db.Departments.AsNoTracking() on lecturer.DepartmentId equals department.Id
                            where plan.Id == id
                            select new { lecturer.AccountId, department.FacultyId, DepartmentId = department.Id })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy kế hoạch NCKH.");
        if (access.AccountId != currentUser.Id && !currentUser.CanAccess(access.FacultyId, access.DepartmentId))
            throw new ForbiddenException("Không có quyền xem lịch sử kế hoạch này.");
        return Ok(await db.ResearchPlanHistories.AsNoTracking()
            .Where(x => x.ResearchPlanId == id)
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => new { x.Id, x.ActorAccountId, x.FromStatus, x.ToStatus, x.Reason, x.OccurredAt })
            .ToListAsync(cancellationToken));
    }

    public sealed record ReopenPlanRequest(string? Reason);
}
