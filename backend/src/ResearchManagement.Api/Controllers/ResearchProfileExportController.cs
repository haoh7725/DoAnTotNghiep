using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Infrastructure.Persistence;

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
            select new { p.Title, TypeName = type.Name, TypeCode = type.Code, p.PublishedDate, p.PublicationInfo };
        if (academicYearId.HasValue)
        {
            var year = await db.AcademicYears.SingleOrDefaultAsync(x => x.Id == academicYearId, ct) ?? throw new NotFoundException("Không tìm thấy năm học.");
            query = query.Where(x => x.PublishedDate >= year.StartDate && x.PublishedDate <= year.EndDate);
        }
        if (!string.IsNullOrWhiteSpace(productTypeCode)) query = query.Where(x => x.TypeCode == productTypeCode);
        var rows = await query.OrderByDescending(x => x.PublishedDate).ThenBy(x => x.Title).ToListAsync(ct);
        var title = $"Sản phẩm NCKH — {lecturer.FullName}";
        if (format == "xlsx")
        {
            using var book = new XLWorkbook(); var sheet = book.Worksheets.Add("San pham NCKH");
            sheet.Cell(1,1).Value = title; sheet.Range(1,1,1,5).Merge().Style.Font.SetBold();
            var headers = new[] { "STT", "Tên sản phẩm", "Loại NCKH", "Ngày công bố/nghiệm thu", "Nơi công bố/đơn vị nghiệm thu" };
            for (var i=0;i<5;i++) sheet.Cell(3,i+1).Value=headers[i];
            var row=4; foreach(var item in rows) {
                sheet.Cell(row,1).Value=row-3; sheet.Cell(row,2).Value=item.Title; sheet.Cell(row,3).Value=item.TypeName;
                sheet.Cell(row,4).Value=item.PublishedDate?.ToString("dd/MM/yyyy") ?? ""; sheet.Cell(row,5).Value=item.PublicationInfo ?? ""; row++;
            }
            sheet.Column(2).Width=65; sheet.Column(3).Width=28; sheet.Column(4).Width=24; sheet.Column(5).Width=45;
            sheet.Range(3,1,Math.Max(3,row-1),5).Style.Alignment.WrapText=true;
            using var stream=new MemoryStream(); book.SaveAs(stream);
            return File(stream.ToArray(),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",$"NCKH-{lecturer.Code}.xlsx");
        }
        var pdf=Document.Create(doc => doc.Page(page => {
            page.Size(PageSizes.A4); page.Margin(30); page.DefaultTextStyle(s=>s.FontSize(10));
            page.Header().Text(title).Bold().FontSize(16);
            page.Content().PaddingTop(12).Column(col => {
                foreach(var type in rows.GroupBy(x=>x.TypeName)) {
                    col.Item().PaddingTop(12).Text(type.Key).Bold();
                    foreach(var row in type) col.Item().PaddingTop(5).Text($"• {row.Title}\n{row.PublishedDate:dd/MM/yyyy} · {row.PublicationInfo}");
                }
                if(rows.Count==0) col.Item().Text("Chưa có sản phẩm được công nhận.");
            });
            page.Footer().AlignCenter().Text(t=>t.CurrentPageNumber());
        })).GeneratePdf();
        return File(pdf,"application/pdf",$"NCKH-{lecturer.Code}.pdf");
    }
}
