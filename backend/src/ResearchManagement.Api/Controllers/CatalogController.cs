using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Api.Contracts;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;
namespace ResearchManagement.Api.Controllers;
[ApiController, Route("api/admin"), Authorize(Policy = Policies.Admin)]
public sealed class CatalogController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("faculties")]
    public async Task<IActionResult> Faculties(CancellationToken ct) => Ok(await db.Faculties.AsNoTracking().OrderBy(x=>x.Code).ToListAsync(ct));
    [HttpPost("faculties")]
    public async Task<IActionResult> CreateFaculty(FacultyRequest r, CancellationToken ct)
    {
        var item = new Faculty(r.Code.Trim(), r.Name.Trim()); db.Add(item); await db.SaveChangesAsync(ct);
        return Created($"/api/admin/faculties/{item.Id}", item);
    }
    [HttpGet("faculties/{id:long}")]
    public async Task<IActionResult> Faculty(long id, CancellationToken ct) => await db.Faculties.FindAsync([id], ct) is { } item ? Ok(item) : NotFound();
    [HttpPut("faculties/{id:long}")]
    public async Task<IActionResult> UpdateFaculty(long id, FacultyRequest r, CancellationToken ct)
    {
        var item = await db.Faculties.FindAsync([id], ct); if (item is null) return NotFound();
        item.Update(r.Code.Trim(), r.Name.Trim()); await db.SaveChangesAsync(ct); return Ok(item);
    }
    [HttpDelete("faculties/{id:long}")]
    public async Task<IActionResult> DeleteFaculty(long id, CancellationToken ct)
    {
        var item = await db.Faculties.FindAsync([id], ct); if (item is null) return NotFound();
        db.Remove(item); await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpGet("departments")]
    public async Task<IActionResult> Departments(CancellationToken ct) => Ok(await db.Departments.AsNoTracking().OrderBy(x=>x.Code).ToListAsync(ct));
    [HttpGet("departments/{id:long}")]
    public async Task<IActionResult> Department(long id, CancellationToken ct) => await db.Departments.FindAsync([id], ct) is { } item ? Ok(item) : NotFound();
    [HttpPost("departments")]
    public async Task<IActionResult> CreateDepartment(DepartmentRequest r, CancellationToken ct)
    {
        var item = new Department { FacultyId = r.FacultyId, Code = r.Code.Trim(), Name = r.Name.Trim() };
        db.Add(item); await db.SaveChangesAsync(ct); return Created($"/api/admin/departments/{item.Id}", item);
    }
    [HttpPut("departments/{id:long}")]
    public async Task<IActionResult> UpdateDepartment(long id, DepartmentRequest r, CancellationToken ct)
    {
        var item = await db.Departments.FindAsync([id], ct); if (item is null) return NotFound();
        item.FacultyId = r.FacultyId; item.Code = r.Code.Trim(); item.Name = r.Name.Trim(); await db.SaveChangesAsync(ct); return Ok(item);
    }
    [HttpDelete("departments/{id:long}")]
    public async Task<IActionResult> DeleteDepartment(long id, CancellationToken ct)
    {
        var item = await db.Departments.FindAsync([id], ct); if (item is null) return NotFound();
        db.Remove(item); await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpGet("academic-years")]
    public async Task<IActionResult> Years(CancellationToken ct) => Ok(await db.AcademicYears.AsNoTracking().OrderByDescending(x=>x.StartDate).ToListAsync(ct));
    [HttpGet("academic-years/{id:long}")]
    public async Task<IActionResult> Year(long id, CancellationToken ct) => await db.AcademicYears.FindAsync([id], ct) is { } item ? Ok(item) : NotFound();
    [HttpPost("academic-years")]
    public async Task<IActionResult> CreateYear(AcademicYearRequest r, CancellationToken ct)
    {
        if (r.StartDate == default || r.EndDate < r.StartDate) return BadRequest(new { title = "Ngày kết thúc phải từ ngày bắt đầu trở đi." });
        var item = new AcademicYear { Code = r.Code.Trim(), StartDate = r.StartDate, EndDate = r.EndDate };
        db.Add(item); await db.SaveChangesAsync(ct); return Created($"/api/admin/academic-years/{item.Id}", item);
    }
    [HttpPut("academic-years/{id:long}")]
    public async Task<IActionResult> UpdateYear(long id, AcademicYearRequest r, CancellationToken ct)
    {
        if (r.StartDate == default || r.EndDate < r.StartDate) return BadRequest(new { title = "Ngày kết thúc phải từ ngày bắt đầu trở đi." });
        var item = await db.AcademicYears.FindAsync([id], ct); if (item is null) return NotFound();
        item.Code = r.Code.Trim(); item.StartDate = r.StartDate; item.EndDate = r.EndDate; await db.SaveChangesAsync(ct); return Ok(item);
    }
    [HttpDelete("academic-years/{id:long}")]
    public async Task<IActionResult> DeleteYear(long id, CancellationToken ct)
    {
        var item = await db.AcademicYears.FindAsync([id], ct); if (item is null) return NotFound();
        db.Remove(item); await db.SaveChangesAsync(ct); return NoContent();
    }
}
