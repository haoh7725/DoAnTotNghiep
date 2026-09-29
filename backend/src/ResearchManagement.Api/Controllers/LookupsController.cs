using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Security;
using ResearchManagement.Application.Security;
using ResearchManagement.Infrastructure.Persistence;
namespace ResearchManagement.Api.Controllers;
[ApiController, Route("api/lookups"), Authorize]
public sealed class LookupsController(ApplicationDbContext db, CurrentSession session) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var permissions = session.Permissions.Where(PermissionRules.IsValid).ToList();
        var school = permissions.Any(p=>p.Scope=="TOAN_TRUONG");
        var faculties = permissions.Where(p=>p.FacultyId.HasValue).Select(p=>p.FacultyId!.Value).ToArray();
        var wholeFaculties = permissions.Where(p=>p.Scope=="KHOA").Select(p=>p.FacultyId!.Value).ToArray();
        var departments = permissions.Where(p=>p.DepartmentId.HasValue).Select(p=>p.DepartmentId!.Value).ToArray();
        // Personal users can see their own organizational unit via the lecturer link.
        var ownDepartments = await db.Database.SqlQuery<long>($"SELECT bo_mon_id AS \"Value\" FROM nckh.giang_vien WHERE tai_khoan_id = {session.Account.Id}").ToListAsync(ct);
        var ownFaculties = await db.Departments.Where(x=>ownDepartments.Contains(x.Id)).Select(x=>x.FacultyId).ToListAsync(ct);
        return Ok(new {
            faculties = await db.Faculties.AsNoTracking().Where(x=>school || faculties.Contains(x.Id) || ownFaculties.Contains(x.Id)).OrderBy(x=>x.Code).ToListAsync(ct),
            departments = await db.Departments.AsNoTracking().Where(x=>school || wholeFaculties.Contains(x.FacultyId) || departments.Contains(x.Id) || ownDepartments.Contains(x.Id)).OrderBy(x=>x.Code).ToListAsync(ct),
            academicYears = await db.AcademicYears.AsNoTracking().OrderByDescending(x=>x.StartDate).ToListAsync(ct)
        });
    }
}
