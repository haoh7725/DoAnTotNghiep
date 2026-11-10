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

    [HttpGet("trends")]
    public async Task<ActionResult<IReadOnlyList<ResearchReportTrend>>> Trends(
        [FromQuery] long[] academicYearIds,
        [FromQuery] long? facultyId,
        [FromQuery] long? departmentId,
        CancellationToken ct)
    {
        if (academicYearIds.Length < 2)
            throw new BusinessRuleException("Phải chọn ít nhất hai năm học để so sánh.");
        if (academicYearIds.Length > 10)
            throw new BusinessRuleException("Chỉ được so sánh tối đa 10 năm học.");
        var result = new List<ResearchReportTrend>();
        foreach (var yearId in academicYearIds.Distinct())
        {
            var report = await BuildAsync(new ResearchReportQuery
            {
                AcademicYearId = yearId,
                FacultyId = facultyId,
                DepartmentId = departmentId
            }, ct);
            result.Add(new ResearchReportTrend(report.AcademicYearId, report.AcademicYearCode,
                report.Totals.Lecturers, report.Totals.Products, report.Totals.Hours, report.Totals.Points));
        }
        return result;
    }

    [HttpGet("export.xlsx")]
    public async Task<IActionResult> Excel([FromQuery] ResearchReportQuery query, CancellationToken ct)
    {
        var report = await BuildAsync(query, ct);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Ket qua NCKH");
        sheet.Cell("A1").Value = "BÁO CÁO KẾT QUẢ NGHIÊN CỨU KHOA HỌC";
        sheet.Range("A1:K1").Merge().Style.Font.SetBold().Font.SetFontSize(16).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        sheet.Cell("A2").Value = $"Năm học: {report.AcademicYearCode}";
        sheet.Range("A2:K2").Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        sheet.Cell("A4").Value = "Tổng giảng viên"; sheet.Cell("B4").Value = report.Totals.Lecturers;
        sheet.Cell("D4").Value = "Tổng sản phẩm"; sheet.Cell("E4").Value = report.Totals.Products;
        sheet.Cell("G4").Value = "Tổng giờ"; sheet.Cell("H4").Value = report.Totals.Hours;
        sheet.Cell("I4").Value = "Tổng điểm"; sheet.Cell("J4").Value = report.Totals.Points;
        var headers = new[] { "Mã GV", "Họ tên", "Bộ môn", "Học hàm", "Học vị", "Xếp loại", "SP tham gia", "SP quy đổi", "Giờ", "Điểm", "Ngày đánh giá" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(6, i + 1).Value = headers[i];
        var row = 7;
        foreach (var item in report.Rows)
        {
            sheet.Cell(row, 1).Value = item.LecturerCode; sheet.Cell(row, 2).Value = item.LecturerName;
            sheet.Cell(row, 3).Value = item.DepartmentName; sheet.Cell(row, 4).Value = item.AcademicRank ?? "";
            sheet.Cell(row, 5).Value = item.Degree ?? ""; sheet.Cell(row, 6).Value = item.Classification ?? "";
            sheet.Cell(row, 7).Value = item.Products; sheet.Cell(row, 8).Value = item.ConvertedProducts;
            sheet.Cell(row, 9).Value = item.Hours; sheet.Cell(row, 10).Value = item.Points;
            sheet.Cell(row, 11).Value = item.EvaluatedAt.HasValue ? item.EvaluatedAt.Value.DateTime : "";
            row++;
        }
        var table = sheet.Range(6, 1, Math.Max(6, row - 1), 11);
        table.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
        sheet.Range(6, 1, 6, 11).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#173F32")).Font.SetFontColor(XLColor.White).Font.SetBold();
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
                    t.ColumnsDefinition(x => { x.ConstantColumn(55); x.RelativeColumn(2); x.RelativeColumn(1.5f); x.RelativeColumn(); x.RelativeColumn(); x.RelativeColumn(); x.ConstantColumn(43); x.ConstantColumn(43); x.ConstantColumn(50); x.ConstantColumn(50); });
                    t.Header(h => { foreach (var text in new[] { "Mã GV", "Họ tên", "Bộ môn", "Học hàm", "Học vị", "Xếp loại", "SP tham gia", "SP quy đổi", "Giờ", "Điểm" }) h.Cell().Background("173F32").Padding(5).Text(text).FontColor(Colors.White).Bold(); });
                    foreach (var x in report.Rows)
                    {
                        foreach (var value in new[] { x.LecturerCode, x.LecturerName, x.DepartmentName, x.AcademicRank ?? "-", x.Degree ?? "-", x.Classification ?? "Chưa chốt", x.Products.ToString(), x.ConvertedProducts.ToString(), x.Hours.ToString("0.####"), x.Points.ToString("0.####") })
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
        var lecturers = await db.Lecturers.AsNoTracking().ToListAsync(ct);
        var evaluations = await db.Evaluations.AsNoTracking().Where(x => x.AcademicYearId == query.AcademicYearId && x.Status == "CHOT").ToListAsync(ct);
        var latest = evaluations.GroupBy(x => x.LecturerId).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Attempt).First());
        var ids = latest.Values.Select(x => x.Id).ToArray();
        var details = await db.EvaluationConversionDetails.AsNoTracking().Where(x => ids.Contains(x.EvaluationId)).ToListAsync(ct);
        var approvedProducts = await (from product in db.Products.AsNoTracking()
                                      join item in db.ResearchPlanItems.AsNoTracking() on product.ResearchPlanItemId equals item.Id
                                      join plan in db.ResearchPlans.AsNoTracking() on item.ResearchPlanId equals plan.Id
                                      where plan.AcademicYearId == query.AcademicYearId && product.Status == "DA_DUYET"
                                      select new { product.Id, product.SubmittedByLecturerId }).ToListAsync(ct);
        var approvedIds = approvedProducts.Select(x => x.Id).ToArray();
        var coAuthors = await db.ProductCoAuthors.AsNoTracking().Where(x => approvedIds.Contains(x.ProductId)).ToListAsync(ct);
        var coAuthorsByProduct = coAuthors.ToLookup(x => x.ProductId);
        var productCounts = new Dictionary<long, HashSet<long>>();
        foreach (var product in approvedProducts)
        {
            if (!productCounts.TryGetValue(product.SubmittedByLecturerId, out var owned)) productCounts[product.SubmittedByLecturerId] = owned = [];
            owned.Add(product.Id);
            foreach (var coAuthor in coAuthorsByProduct[product.Id])
            {
                if (!productCounts.TryGetValue(coAuthor.LecturerId, out var shared)) productCounts[coAuthor.LecturerId] = shared = [];
                shared.Add(product.Id);
            }
        }
        var rows = new List<ResearchReportRow>();
        // Báo cáo phải phản ánh toàn bộ giảng viên trong phạm vi, kể cả người chưa
        // có sản phẩm hoặc chưa được chốt đánh giá (các chỉ số của họ bằng 0).
        foreach (var lecturer in lecturers)
        {
            latest.TryGetValue(lecturer.Id, out var evaluation);
            var departmentId = evaluation?.DepartmentIdSnapshot ?? lecturer.DepartmentId;
            if (!departments.TryGetValue(departmentId, out var department)) continue;
            var academicRank = evaluation?.AcademicRankSnapshot ?? lecturer.AcademicRank;
            var degree = evaluation?.DegreeSnapshot ?? lecturer.Degree;
            if (!currentUser.CanAccess(department.FacultyId, department.Id)
                || (query.FacultyId.HasValue && department.FacultyId != query.FacultyId)
                || (query.DepartmentId.HasValue && department.Id != query.DepartmentId)
                || (!string.IsNullOrWhiteSpace(query.AcademicRank) && !string.Equals(academicRank, query.AcademicRank, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(query.Degree) && !string.Equals(degree, query.Degree, StringComparison.OrdinalIgnoreCase))) continue;
            var own = evaluation is null
                ? new List<ResearchManagement.Domain.Entities.EvaluationConversionDetail>()
                : details.Where(d => d.EvaluationId == evaluation.Id).ToList();
            rows.Add(new ResearchReportRow(lecturer.Id, evaluation?.LecturerCodeSnapshot ?? lecturer.Code,
                evaluation?.LecturerNameSnapshot ?? lecturer.FullName, departmentId, department.Name, academicRank, degree,
                evaluation?.Classification, productCounts.GetValueOrDefault(lecturer.Id)?.Count ?? 0,
                own.Select(d => d.ProductId).Distinct().Count(), own.Where(d => d.Unit == "GIO").Sum(d => d.ConvertedValue),
                own.Where(d => d.Unit == "DIEM").Sum(d => d.ConvertedValue), evaluation?.EvaluatedAt));
        }
        rows = rows.OrderBy(x => x.DepartmentName).ThenBy(x => x.LecturerName).ToList();
        var uniqueProducts = rows.SelectMany(x => productCounts.GetValueOrDefault(x.LecturerId) ?? []).Distinct().Count();
        var totals = new ResearchReportTotals(rows.Count, uniqueProducts, rows.Sum(x => x.Hours), rows.Sum(x => x.Points));
        return new ResearchReportResponse(query.AcademicYearId, year.Code, totals,
            Group(rows, x => x.AcademicRank ?? "Chưa có học hàm", productCounts),
            Group(rows, x => x.Degree ?? "Chưa có học vị", productCounts),
            Group(rows, x => x.DepartmentName, productCounts), rows);
    }

    private static IReadOnlyList<ResearchReportGroup> Group(IEnumerable<ResearchReportRow> rows,
        Func<ResearchReportRow, string> key, IReadOnlyDictionary<long, HashSet<long>> productIds) =>
        rows.GroupBy(key).OrderBy(x => x.Key).Select(g => new ResearchReportGroup(g.Key, g.Count(),
            g.SelectMany(x => productIds.GetValueOrDefault(x.LecturerId) ?? []).Distinct().Count(),
            g.Sum(x => x.Hours), g.Sum(x => x.Points))).ToList();
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
public sealed record ResearchReportRow(long LecturerId, string LecturerCode, string LecturerName, long DepartmentId, string DepartmentName, string? AcademicRank, string? Degree, string? Classification, int Products, int ConvertedProducts, decimal Hours, decimal Points, DateTimeOffset? EvaluatedAt);
public sealed record ResearchReportResponse(long AcademicYearId, string AcademicYearCode, ResearchReportTotals Totals, IReadOnlyList<ResearchReportGroup> ByAcademicRank, IReadOnlyList<ResearchReportGroup> ByDegree, IReadOnlyList<ResearchReportGroup> ByDepartment, IReadOnlyList<ResearchReportRow> Rows);
public sealed record ResearchReportTrend(long AcademicYearId, string AcademicYearCode, int Lecturers, int Products, decimal Hours, decimal Points);
