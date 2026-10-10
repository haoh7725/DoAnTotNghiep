using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Controllers;

[ApiController, Route("api/reports/research-results"), Authorize(Policy = Policies.Management)]
public sealed class ReportsController(ApplicationDbContext db, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ResearchReportResponse>> Get([FromQuery] ResearchReportQuery query, CancellationToken ct) =>
        await BuildAsync(query, ct);

    [HttpGet("export.xlsx")]
    public async Task<IActionResult> Excel([FromQuery] ResearchReportQuery query, CancellationToken ct)
    {
        var report = await BuildAsync(query, ct);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Ket qua NCKH");
        sheet.Cell("A1").Value = "BÁO CÁO KẾT QUẢ NGHIÊN CỨU KHOA HỌC";
        sheet.Range("A1:J1").Merge().Style.Font.SetBold().Font.SetFontSize(16).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        sheet.Cell("A2").Value = $"Năm học: {report.AcademicYearCode}";
        sheet.Range("A2:J2").Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        sheet.Cell("A4").Value = "Tổng giảng viên"; sheet.Cell("B4").Value = report.Totals.Lecturers;
        sheet.Cell("D4").Value = "Tổng sản phẩm"; sheet.Cell("E4").Value = report.Totals.Products;
        sheet.Cell("G4").Value = "Tổng giờ"; sheet.Cell("H4").Value = report.Totals.Hours;
        sheet.Cell("I4").Value = "Tổng điểm"; sheet.Cell("J4").Value = report.Totals.Points;
        var headers = new[] { "Mã GV", "Họ tên", "Bộ môn", "Học hàm", "Học vị", "Xếp loại", "Sản phẩm", "Giờ", "Điểm", "Ngày đánh giá" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(6, i + 1).Value = headers[i];
        var row = 7;
        foreach (var item in report.Rows)
        {
            sheet.Cell(row, 1).Value = item.LecturerCode; sheet.Cell(row, 2).Value = item.LecturerName;
            sheet.Cell(row, 3).Value = item.DepartmentName; sheet.Cell(row, 4).Value = item.AcademicRank ?? "";
            sheet.Cell(row, 5).Value = item.Degree ?? ""; sheet.Cell(row, 6).Value = item.Classification ?? "";
            sheet.Cell(row, 7).Value = item.Products; sheet.Cell(row, 8).Value = item.Hours;
            sheet.Cell(row, 9).Value = item.Points; sheet.Cell(row, 10).Value = item.EvaluatedAt.DateTime;
            row++;
        }
        var table = sheet.Range(6, 1, Math.Max(6, row - 1), 10);
        table.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
        sheet.Range(6, 1, 6, 10).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#173F32")).Font.SetFontColor(XLColor.White).Font.SetBold();
        sheet.Columns().AdjustToContents(); sheet.Column(2).Width = Math.Max(sheet.Column(2).Width, 24);
        sheet.SheetView.FreezeRows(6); sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        using var stream = new MemoryStream(); workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ket-qua-nckh-{report.AcademicYearCode}.xlsx");
    }

    [HttpGet("export.pdf")]
    public async Task<IActionResult> Pdf([FromQuery] ResearchReportQuery query, CancellationToken ct)
    {
        var report = await BuildAsync(query, ct);
        var bytes = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape()); page.Margin(28); page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Column(c => { c.Item().AlignCenter().Text("BÁO CÁO KẾT QUẢ NGHIÊN CỨU KHOA HỌC").Bold().FontSize(16); c.Item().AlignCenter().Text($"Năm học {report.AcademicYearCode}"); });
            page.Content().PaddingVertical(16).Column(c =>
            {
                c.Spacing(12); c.Item().Row(r => { r.RelativeItem().Text($"Giảng viên: {report.Totals.Lecturers}"); r.RelativeItem().Text($"Sản phẩm: {report.Totals.Products}"); r.RelativeItem().Text($"Giờ: {report.Totals.Hours:0.####}"); r.RelativeItem().Text($"Điểm: {report.Totals.Points:0.####}"); });
                c.Item().Table(t =>
                {
                    t.ColumnsDefinition(x => { x.ConstantColumn(55); x.RelativeColumn(2); x.RelativeColumn(1.5f); x.RelativeColumn(); x.RelativeColumn(); x.RelativeColumn(); x.ConstantColumn(48); x.ConstantColumn(55); x.ConstantColumn(55); });
                    t.Header(h => { foreach (var text in new[] { "Mã GV", "Họ tên", "Bộ môn", "Học hàm", "Học vị", "Xếp loại", "SP", "Giờ", "Điểm" }) h.Cell().Background("173F32").Padding(5).Text(text).FontColor(Colors.White).Bold(); });
                    foreach (var x in report.Rows)
                    {
                        foreach (var value in new[] { x.LecturerCode, x.LecturerName, x.DepartmentName, x.AcademicRank ?? "-", x.Degree ?? "-", x.Classification ?? "-", x.Products.ToString(), x.Hours.ToString("0.####"), x.Points.ToString("0.####") })
                            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(value);
                    }
                });
            });
            page.Footer().AlignCenter().Text(x => { x.Span("Trang "); x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
        })).GeneratePdf();
        return File(bytes, "application/pdf", $"ket-qua-nckh-{report.AcademicYearCode}.pdf");
    }

    private async Task<ResearchReportResponse> BuildAsync(ResearchReportQuery query, CancellationToken ct)
    {
        if (query.AcademicYearId <= 0) throw new BusinessRuleException("Phải chọn năm học.");
        var year = await db.AcademicYears.AsNoTracking().SingleOrDefaultAsync(x => x.Id == query.AcademicYearId, ct)
            ?? throw new NotFoundException("Không tìm thấy năm học.");
        var departments = await db.Departments.AsNoTracking().ToDictionaryAsync(x => x.Id, ct);
        var evaluations = await db.Evaluations.AsNoTracking().Where(x => x.AcademicYearId == query.AcademicYearId && x.Status == "CHOT").ToListAsync(ct);
        var latest = evaluations.GroupBy(x => x.LecturerId).Select(g => g.OrderByDescending(x => x.Attempt).First()).ToList();
        latest = latest.Where(x => departments.TryGetValue(x.DepartmentIdSnapshot, out var d) && currentUser.CanAccess(d.FacultyId, d.Id)
            && (!query.FacultyId.HasValue || d.FacultyId == query.FacultyId)
            && (!query.DepartmentId.HasValue || d.Id == query.DepartmentId)
            && (string.IsNullOrWhiteSpace(query.AcademicRank) || string.Equals(x.AcademicRankSnapshot, query.AcademicRank, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(query.Degree) || string.Equals(x.DegreeSnapshot, query.Degree, StringComparison.OrdinalIgnoreCase))).ToList();
        var ids = latest.Select(x => x.Id).ToArray();
        var details = await db.EvaluationConversionDetails.AsNoTracking().Where(x => ids.Contains(x.EvaluationId)).ToListAsync(ct);
        var rows = latest.Select(x =>
        {
            var own = details.Where(d => d.EvaluationId == x.Id).ToList();
            return new ResearchReportRow(x.LecturerId, x.LecturerCodeSnapshot, x.LecturerNameSnapshot,
                x.DepartmentIdSnapshot, departments[x.DepartmentIdSnapshot].Name, x.AcademicRankSnapshot, x.DegreeSnapshot,
                x.Classification, own.Select(d => d.ProductId).Distinct().Count(), own.Where(d => d.Unit == "GIO").Sum(d => d.ConvertedValue),
                own.Where(d => d.Unit == "DIEM").Sum(d => d.ConvertedValue), x.EvaluatedAt);
        }).OrderBy(x => x.DepartmentName).ThenBy(x => x.LecturerName).ToList();
        var totals = new ResearchReportTotals(rows.Count, rows.Sum(x => x.Products), rows.Sum(x => x.Hours), rows.Sum(x => x.Points));
        return new ResearchReportResponse(query.AcademicYearId, year.Code, totals,
            Group(rows, x => x.AcademicRank ?? "Chưa có học hàm"), Group(rows, x => x.Degree ?? "Chưa có học vị"),
            Group(rows, x => x.DepartmentName), rows);
    }

    private static IReadOnlyList<ResearchReportGroup> Group(IEnumerable<ResearchReportRow> rows, Func<ResearchReportRow, string> key) =>
        rows.GroupBy(key).OrderBy(x => x.Key).Select(g => new ResearchReportGroup(g.Key, g.Count(), g.Sum(x => x.Products), g.Sum(x => x.Hours), g.Sum(x => x.Points))).ToList();
}

public sealed class ResearchReportQuery
{
    public long AcademicYearId { get; set; }
    public long? FacultyId { get; set; }
    public long? DepartmentId { get; set; }
    public string? AcademicRank { get; set; }
    public string? Degree { get; set; }
}
public sealed record ResearchReportTotals(int Lecturers, int Products, decimal Hours, decimal Points);
public sealed record ResearchReportGroup(string Name, int Lecturers, int Products, decimal Hours, decimal Points);
public sealed record ResearchReportRow(long LecturerId, string LecturerCode, string LecturerName, long DepartmentId, string DepartmentName, string? AcademicRank, string? Degree, string? Classification, int Products, decimal Hours, decimal Points, DateTimeOffset EvaluatedAt);
public sealed record ResearchReportResponse(long AcademicYearId, string AcademicYearCode, ResearchReportTotals Totals, IReadOnlyList<ResearchReportGroup> ByAcademicRank, IReadOnlyList<ResearchReportGroup> ByDegree, IReadOnlyList<ResearchReportGroup> ByDepartment, IReadOnlyList<ResearchReportRow> Rows);
