using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Infrastructure.Persistence;
using ResearchManagement.Api.Infrastructure;
using ResearchManagement.Application.Lecturers.Models;
using System.Text.Json;

namespace ResearchManagement.Api.Controllers;

[ApiController, Authorize, Route("api/lecturers/{lecturerId:long}/research-export")]
public sealed class ResearchProfileExportController(ApplicationDbContext db, ICurrentUser user) : ControllerBase
{
    [HttpGet("{format}")]
    public async Task<IActionResult> Export(long lecturerId, string format, long? academicYearId, string? productTypeCode, CancellationToken ct)
    {
        if (format is not ("xlsx" or "pdf")) throw new BusinessRuleException("Chọn Excel hoặc PDF.");
        var lecturer = await db.Lecturers.SingleOrDefaultAsync(x => x.Id == lecturerId, ct) ?? throw new NotFoundException("Không tìm thấy giảng viên.");
        var facultyId = await db.Departments.Where(x => x.Id == lecturer.DepartmentId).Select(x => x.FacultyId).SingleAsync(ct);
        if (lecturer.AccountId != user.Id && !user.CanAccess(facultyId, lecturer.DepartmentId)) throw new ForbiddenException("Không có quyền xuất hồ sơ này.");
        var query = from p in db.Products.AsNoTracking() join item in db.ResearchPlanItems on p.ResearchPlanItemId equals item.Id
            join type in db.ProductTypes on item.ProductTypeId equals type.Id
            where p.Status == "DA_DUYET" && type.Code != "CHUNG_NHAN" && (p.SubmittedByLecturerId == lecturerId || db.ProductCoAuthors.Any(a => a.ProductId == p.Id && a.LecturerId == lecturerId))
            select new { p.Title, TypeName = type.Name, TypeCode = type.Code, p.PublishedDate, p.PublicationInfo,
                p.ProjectLevel, AuthorRole = p.SubmittedByLecturerId == lecturerId ? "TAC_GIA_CHINH" : db.ProductCoAuthors
                    .Where(a => a.ProductId == p.Id && a.LecturerId == lecturerId).Select(a => a.AuthorRole).FirstOrDefault() };
        if (academicYearId.HasValue)
        {
            var year = await db.AcademicYears.SingleOrDefaultAsync(x => x.Id == academicYearId, ct) ?? throw new NotFoundException("Không tìm thấy năm học.");
            query = query.Where(x => x.PublishedDate >= year.StartDate && x.PublishedDate <= year.EndDate);
        }
        if (!string.IsNullOrWhiteSpace(productTypeCode)) query = query.Where(x => x.TypeCode == productTypeCode);
        var rows = await query.OrderByDescending(x => x.PublishedDate).ThenBy(x => x.Title).ToListAsync(ct);
        var profile = await db.ScientificProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.LecturerId == lecturerId, ct);
        var details = JsonSerializer.Deserialize<ScientificProfileDetails>(profile?.DetailsJson ?? "{}", new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new();
        var department = await db.Departments.AsNoTracking().SingleAsync(x => x.Id == lecturer.DepartmentId, ct);
        var sections = ScientificProfileDocument.Build(lecturer, department.Name, details, profile,
            rows.Select(x => new ScientificProfileDocument.Product(x.Title, x.TypeName, x.TypeCode, x.PublishedDate, x.PublicationInfo, x.ProjectLevel, x.AuthorRole)).ToList());
        if (format == "xlsx")
        {
            return File(ScientificProfileDocument.Excel(sections),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",$"NCKH-{lecturer.Code}.xlsx");
        }
        var pdf = ScientificProfileDocument.Pdf(sections);
        return File(pdf,"application/pdf",$"NCKH-{lecturer.Code}.pdf");
    }
}
