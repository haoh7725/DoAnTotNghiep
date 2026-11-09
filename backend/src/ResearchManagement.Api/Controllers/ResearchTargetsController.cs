using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchTargets;
using ResearchManagement.Application.ResearchTargets.Models;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Infrastructure.Persistence;
namespace ResearchManagement.Api.Controllers;



[ApiController]
[Route("api/research-targets")]
[Authorize]
public sealed class ResearchTargetsController(
    ResearchTargetService researchTargetService,
    ApplicationDbContext db,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Management)]
    public async Task<ActionResult<List<ResearchTargetResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await researchTargetService.GetAllAsync(cancellationToken);
        var visible = new List<ResearchTargetResponse>();
        foreach (var item in result)
        {
            if (await CanReadTargetAsync(item.Id, item.FacultyId, cancellationToken))
                visible.Add(item);
        }
        return Ok(visible);
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = Policies.Management)]
    public async Task<ActionResult<ResearchTargetResponse>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await researchTargetService.GetByIdAsync(
            id,
            cancellationToken);

        if (!await CanReadTargetAsync(result.Id, result.FacultyId, cancellationToken))
            throw new ForbiddenException("Không có quyền xem chỉ tiêu NCKH này.");

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "TRUONG_KHOA,PHONG_QLKH,QUAN_TRI")]
    public async Task<ActionResult<ResearchTargetResponse>> Create(
        CreateResearchTargetRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.CanAccess(request.FacultyId, null))
            throw new ForbiddenException("Không có quyền tạo chỉ tiêu cho khoa này.");
        if (!await db.Faculties.AnyAsync(x => x.Id == request.FacultyId, cancellationToken))
            throw new NotFoundException("Không tìm thấy khoa.");
        var year = await db.AcademicYears.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.AcademicYearId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy năm học.");
        if (request.Deadline.HasValue &&
            (request.Deadline.Value < year.StartDate || request.Deadline.Value > year.EndDate))
            throw new BusinessRuleException("Hạn hoàn thành chỉ tiêu phải nằm trong năm học.");
        if (request.Deadline.HasValue && request.Deadline.Value < CurrentVietnamDate())
            throw new BusinessRuleException("Hạn hoàn thành chỉ tiêu không được nằm trong quá khứ.");
        if (request.JournalConferenceId.HasValue && !await db.JournalConferences.AnyAsync(x => x.Id == request.JournalConferenceId, cancellationToken))
            throw new NotFoundException("Không tìm thấy tạp chí/hội nghị.");

        var result = await researchTargetService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "TRUONG_KHOA,PHONG_QLKH,QUAN_TRI")]
    public async Task<ActionResult<ResearchTargetResponse>> Update(
        long id,
        UpdateResearchTargetRequest request,
        CancellationToken cancellationToken)
    {
        var target = await db.ResearchTargets.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy chỉ tiêu NCKH.");
        if (!currentUser.CanAccess(target.FacultyId, null))
            throw new ForbiddenException("Không có quyền cập nhật chỉ tiêu của khoa này.");
        var year = await db.AcademicYears.AsNoTracking()
            .SingleAsync(x => x.Id == target.AcademicYearId, cancellationToken);
        if (request.Deadline.HasValue &&
            (request.Deadline.Value < year.StartDate || request.Deadline.Value > year.EndDate))
            throw new BusinessRuleException("Hạn hoàn thành chỉ tiêu phải nằm trong năm học.");
        if (request.Deadline.HasValue && request.Deadline.Value < CurrentVietnamDate())
            throw new BusinessRuleException("Hạn hoàn thành chỉ tiêu không được nằm trong quá khứ.");
        if (request.JournalConferenceId.HasValue && !await db.JournalConferences.AnyAsync(x => x.Id == request.JournalConferenceId, cancellationToken))
            throw new NotFoundException("Không tìm thấy tạp chí/hội nghị.");

        var result = await researchTargetService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:long}/accept")]
    [Authorize(Roles = "TRUONG_KHOA,QUAN_TRI")]
    public async Task<IActionResult> Accept(long id, CancellationToken cancellationToken)
    {
        var target = await GetManageableTarget(id, cancellationToken);
        try { target.Accept(currentUser.Id!.Value, DateTimeOffset.UtcNow); }
        catch (InvalidOperationException e) { throw new BusinessRuleException(e.Message); }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await researchTargetService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost("{id:long}/publish")]
    [Authorize(Roles = "TRUONG_KHOA,PHONG_QLKH,QUAN_TRI")]
    public async Task<IActionResult> Publish(long id, CancellationToken cancellationToken)
    {
        var target = await GetManageableTarget(id, cancellationToken);
        try { target.Publish(DateTimeOffset.UtcNow); }
        catch (InvalidOperationException e) { throw new BusinessRuleException(e.Message); }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await researchTargetService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost("{id:long}/lock")]
    [Authorize(Roles = "TRUONG_KHOA,PHONG_QLKH,QUAN_TRI")]
    public async Task<IActionResult> Lock(long id, CancellationToken cancellationToken)
    {
        var target = await GetManageableTarget(id, cancellationToken);
        var allocated = await db.ResearchTargetAllocations.Where(x => x.ResearchTargetId == id)
            .SumAsync(x => x.AllocatedArticleCount, cancellationToken);
        if (allocated != target.AssignedArticleCount)
            throw new BusinessRuleException("Chỉ được khóa khi tổng phân bổ bằng đúng chỉ tiêu được giao.");
        try { target.Lock(DateTimeOffset.UtcNow); }
        catch (InvalidOperationException e) { throw new BusinessRuleException(e.Message); }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await researchTargetService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost("{id:long}/cancel")]
    [Authorize(Roles = "TRUONG_KHOA,PHONG_QLKH,QUAN_TRI")]
    public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
    {
        var target = await GetManageableTarget(id, cancellationToken);
        if (await db.ResearchTargetAllocations.AnyAsync(x => x.ResearchTargetId == id, cancellationToken))
            throw new BusinessRuleException("Không thể hủy chỉ tiêu đã có phân bổ.");
        try { target.Cancel(); }
        catch (InvalidOperationException e) { throw new BusinessRuleException(e.Message); }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await researchTargetService.GetByIdAsync(id, cancellationToken));
    }

    private async Task<ResearchManagement.Domain.Entities.ResearchTarget> GetManageableTarget(long id, CancellationToken ct)
    {
        var target = await db.ResearchTargets.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy chỉ tiêu NCKH.");
        if (!currentUser.CanAccess(target.FacultyId, null))
            throw new ForbiddenException("Không có quyền quản lý chỉ tiêu của khoa này.");
        return target;
    }

    private async Task<bool> CanReadTargetAsync(long targetId, long facultyId, CancellationToken cancellationToken)
    {
        if (currentUser.CanAccess(facultyId, null))
            return true;

        var units = await (from allocation in db.ResearchTargetAllocations.AsNoTracking()
                           join department in db.Departments.AsNoTracking() on allocation.DepartmentId equals department.Id
                           where allocation.ResearchTargetId == targetId
                           select new { department.FacultyId, DepartmentId = department.Id })
            .ToListAsync(cancellationToken);

        return units.Any(x => currentUser.CanAccess(x.FacultyId, x.DepartmentId));
    }

    private static DateOnly CurrentVietnamDate() =>
        DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                DateTimeOffset.UtcNow,
                OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh").DateTime);
}
