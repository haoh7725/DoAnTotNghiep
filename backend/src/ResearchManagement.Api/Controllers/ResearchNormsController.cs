using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchNorms;
using ResearchManagement.Application.ResearchNorms.Models;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/research-norms")]
[Authorize]
public sealed class ResearchNormsController(
    ResearchNormService researchNormService,
    ApplicationDbContext db,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResearchNormResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await researchNormService.GetAllAsync(
            cancellationToken);

        var visible = new List<ResearchNormResponse>();
        foreach (var item in result)
        {
            if (await CanReadLecturerAsync(item.LecturerId, cancellationToken))
                visible.Add(item);
        }

        return Ok(visible);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ResearchNormResponse>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await researchNormService.GetByIdAsync(
            id,
            cancellationToken);

        if (!await CanReadLecturerAsync(result.LecturerId, cancellationToken))
            throw new ForbiddenException("Không có quyền xem định mức NCKH này.");

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<ResearchNormResponse>> Create(
        CreateResearchNormRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCanManageLecturerAsync(request.LecturerId, cancellationToken);

        var result = await researchNormService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<ResearchNormResponse>> Update(
        long id,
        UpdateResearchNormRequest request,
        CancellationToken cancellationToken)
    {
        var norm = await db.ResearchNorms.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy định mức NCKH.");
        await EnsureCanManageLecturerAsync(norm.LecturerId, cancellationToken);

        var result = await researchNormService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }

    private async Task<bool> CanReadLecturerAsync(long lecturerId, CancellationToken cancellationToken)
    {
        var lecturer = await (from l in db.Lecturers.AsNoTracking()
                              join d in db.Departments.AsNoTracking() on l.DepartmentId equals d.Id
                              where l.Id == lecturerId
                              select new { l.AccountId, d.FacultyId, DepartmentId = d.Id })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");

        return currentUser.Id == lecturer.AccountId ||
               currentUser.CanAccess(lecturer.FacultyId, lecturer.DepartmentId);
    }

    private async Task EnsureCanManageLecturerAsync(long lecturerId, CancellationToken cancellationToken)
    {
        var lecturer = await (from l in db.Lecturers.AsNoTracking()
                              join d in db.Departments.AsNoTracking() on l.DepartmentId equals d.Id
                              where l.Id == lecturerId
                              select new { d.FacultyId, DepartmentId = d.Id })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");

        if (!currentUser.CanAccess(lecturer.FacultyId, lecturer.DepartmentId))
            throw new ForbiddenException("Không có quyền quản lý định mức của giảng viên này.");
    }
}
