using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchTargetAllocations;
using ResearchManagement.Application.ResearchTargetAllocations.Models;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Infrastructure.Persistence;
namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/research-target-allocations")]
[Authorize]
public sealed class ResearchTargetAllocationsController(
    ResearchTargetAllocationService service,
    ApplicationDbContext db,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("{id:long}")]
    [Authorize(Policy = Policies.Management)]
    public async Task<ActionResult<ResearchTargetAllocationResponse>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        EnsureCanRead(result.FacultyId, result.DepartmentId);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = Policies.Management)]
    public async Task<ActionResult<List<ResearchTargetAllocationResponse>>> GetByResearchTarget(
        [FromQuery] long researchTargetId,
        CancellationToken cancellationToken)
    {
        var target = await db.ResearchTargets.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == researchTargetId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy chỉ tiêu NCKH.");
        var result = await service.GetByResearchTargetIdAsync(
            researchTargetId,
            cancellationToken);

        if (currentUser.CanAccess(target.FacultyId, null))
            return Ok(result);

        return Ok(result.Where(x => currentUser.CanAccess(x.FacultyId, x.DepartmentId)).ToList());
    }

    [HttpPost]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<ResearchTargetAllocationResponse>> Create(
        CreateResearchTargetAllocationRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureValidAllocationAsync(request.ResearchTargetId, request.FacultyId, request.DepartmentId, cancellationToken);

        var result = await service.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<ResearchTargetAllocationResponse>> Update(
        long id,
        UpdateResearchTargetAllocationRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await service.GetByIdAsync(id, cancellationToken);
        EnsureCanRead(existing.FacultyId, existing.DepartmentId);

        var result = await service.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }

    private void EnsureCanRead(long facultyId, long departmentId)
    {
        if (!currentUser.CanAccess(facultyId, departmentId))
            throw new ForbiddenException("Không có quyền truy cập phân bổ chỉ tiêu này.");
    }

    private async Task EnsureValidAllocationAsync(
        long researchTargetId,
        long facultyId,
        long departmentId,
        CancellationToken cancellationToken)
    {
        var target = await db.ResearchTargets.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == researchTargetId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy chỉ tiêu NCKH.");
        var department = await db.Departments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == departmentId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy bộ môn.");

        if (target.FacultyId != facultyId || department.FacultyId != facultyId)
            throw new BusinessRuleException("Khoa của chỉ tiêu và bộ môn phân bổ phải trùng khớp.");
        if (!currentUser.CanAccess(facultyId, departmentId))
            throw new ForbiddenException("Không có quyền phân bổ chỉ tiêu cho bộ môn này.");
    }
}
