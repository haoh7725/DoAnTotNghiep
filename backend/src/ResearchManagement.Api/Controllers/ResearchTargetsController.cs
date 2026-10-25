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
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<ResearchTargetResponse>> Create(
        CreateResearchTargetRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.CanAccess(request.FacultyId, null))
            throw new ForbiddenException("Không có quyền tạo chỉ tiêu cho khoa này.");
        if (!await db.Faculties.AnyAsync(x => x.Id == request.FacultyId, cancellationToken))
            throw new NotFoundException("Không tìm thấy khoa.");

        var result = await researchTargetService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Policies.ResearchOffice)]
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

        var result = await researchTargetService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
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
}
