using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResearchManagement.Application.Lecturers.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Api.Infrastructure;

// Shared content keeps both download formats complete and in the same order.
public static class ScientificProfileDocument
{
    public sealed record Product(string Title, string TypeName, string TypeCode, DateOnly? PublishedDate,
        string? PublicationInfo, string? ProjectLevel, string? AuthorRole);
    public sealed record Section(string Title, string[] Headers, int[] Widths, List<string[]> Rows);
    private static string Text(object? value) => value?.ToString() is { Length: > 0 } text ? text : "—";
    private static string Date(DateOnly? value) => value?.ToString("dd/MM/yyyy") ?? "—";
    private static string Role(string? value) => value switch
    {
        "TAC_GIA_CHINH" => "Tác giả chính", "DONG_TAC_GIA" => "Đồng tác giả",
        "TAC_GIA_LIEN_HE" => "Tác giả liên hệ", _ => Text(value)
    };

    public static List<Section> Build(Lecturer l, string department, ScientificProfileDetails d,
        ScientificProfile? profile, IReadOnlyList<Product> products)
    {
        var sections = new List<Section>();
        void Add(string title, string[] headers, int[] widths, IEnumerable<string[]> rows)
            => sections.Add(new(title, headers, widths, rows.ToList()));
        Add("I. LÝ LỊCH SƠ LƯỢC", ["Thông tin", "Nội dung"], [6, 14], (string[][])
        [
            ["Họ và tên", l.FullName], ["Giới tính", l.Gender switch { "NAM" => "Nam", "NU" => "Nữ", "KHAC" => "Khác", _ => Text(l.Gender) }],
            ["Ngày sinh", Date(l.BirthDate)], ["Nơi sinh", Text(d.BirthPlace)], ["Quê quán", Text(d.Hometown)], ["Dân tộc", Text(d.Ethnicity)],
            ["Học vị cao nhất", Text(l.Degree)], ["Năm, nước nhận học vị", $"{Text(d.HighestDegreeYear)}, {Text(d.HighestDegreeCountry)}"],
            ["Chức danh khoa học cao nhất", Text(l.AcademicRank)], ["Năm bổ nhiệm", Text(d.AcademicRankYear)],
            ["Chức vụ", Text(l.Position)], ["Đơn vị công tác", Text(d.WorkUnit)], ["Bộ môn", department],
            ["Địa chỉ liên lạc", Text(d.ContactAddress)], ["Điện thoại cơ quan / nhà riêng / di động", $"{Text(d.OfficePhone)} / {Text(d.HomePhone)} / {Text(l.Phone)}"],
            ["Fax / Email", $"{Text(d.Fax)} / {Text(l.Email)}"], ["Số CMND / CCCD", Text(d.IdentityNumber)],
            ["Ngày cấp / Nơi cấp", $"{Date(d.IdentityIssuedDate)} / {Text(d.IdentityIssuedPlace)}"]
        ]);
        Add("II. QUÁ TRÌNH ĐÀO TẠO", ["Bậc / Hệ đào tạo", "Nơi đào tạo / Nước", "Chuyên ngành", "Năm cấp bằng", "Tên luận án"], [4, 5, 4, 2, 5],
            d.Education.Select(e => new[] { $"{e.Level switch { "DAI_HOC" => "Đại học", "DAI_HOC_2" => "Bằng đại học 2", "THAC_SI" => "Thạc sĩ", "TIEN_SI" => "Tiến sĩ", _ => e.Level }}\n{Text(e.TrainingMode)}",
                $"{e.Institution}\n{Text(e.Country)}", e.Specialization, Text(e.GraduationYear), Text(e.ThesisTitle) }));
        Add("Ngoại ngữ", ["Ngoại ngữ", "Mức độ sử dụng"], [6, 14], d.Languages.Select(e => new[] { e.Name, e.Proficiency }));
        Add("III. QUÁ TRÌNH CÔNG TÁC CHUYÊN MÔN", ["Thời gian", "Nơi công tác", "Công việc đảm nhiệm"], [5, 7, 8],
            d.Employment.Select(e => new[] { $"{Date(e.FromDate)} - {Date(e.ToDate)}", e.Organization, e.Position }));
        Add("IV. QUÁ TRÌNH NGHIÊN CỨU KHOA HỌC\n1. Đề tài đã và đang tham gia (kê khai)",
            ["TT", "Tên đề tài", "Năm bắt đầu / hoàn thành", "Cấp đề tài", "Trách nhiệm tham gia"], [1, 8, 3, 4, 4],
            d.ResearchProjects.Select((e, i) => new[] { (i + 1).ToString(), e.Title, $"{Text(e.StartYear)} / {Text(e.EndYear)}", e.Level, e.Responsibility }));
        Add("2. Công trình khoa học đã công bố (kê khai)", ["TT", "Tên công trình", "Năm công bố", "Loại / Nơi công bố"], [1, 9, 3, 7],
            d.Publications.Select((e, i) => new[] { (i + 1).ToString(), e.Title, Text(e.Year), e.Journal }));
        var groups = new[] { ("DE_TAI", "3. Đề tài nghiên cứu khoa học đã công nhận"), ("BAI_BAO", "4. Bài báo khoa học đã công nhận"),
            ("HOI_NGHI_HOI_THAO", "5. Hội nghị / Hội thảo đã công nhận"), ("SACH", "6. Sách và giáo trình đã công nhận") };
        foreach (var (code, title) in groups)
            Add(title, ["TT", "Tên sản phẩm", "Ngày công bố / nghiệm thu", code == "DE_TAI" ? "Cấp đề tài / Đơn vị nghiệm thu" : "Nơi công bố", "Vai trò"], [1, 8, 3, 5, 3],
                products.Where(p => p.TypeCode == code).Select((p, i) => new[] { (i + 1).ToString(), p.Title, Date(p.PublishedDate),
                    code == "DE_TAI" ? $"{Text(p.ProjectLevel)}\n{Text(p.PublicationInfo)}" : Text(p.PublicationInfo), Role(p.AuthorRole) }));
        var others = products.Where(p => !groups.Any(g => g.Item1 == p.TypeCode)).ToList();
        if (others.Count > 0)
            Add("Sản phẩm được công nhận khác", ["TT", "Tên sản phẩm", "Loại", "Ngày công bố", "Nơi công bố / Vai trò"], [1, 8, 3, 3, 5],
                others.Select((p, i) => new[] { (i + 1).ToString(), p.Title, p.TypeName, Date(p.PublishedDate), $"{Text(p.PublicationInfo)}\n{Role(p.AuthorRole)}" }));
        if (profile is not null && new[] { profile.Expertise, profile.ResearchFields, profile.ResearchDirections, profile.ActivitySummary }.Any(x => !string.IsNullOrWhiteSpace(x)))
            Add("Thông tin chuyên môn và hoạt động khoa học", ["Thông tin", "Nội dung"], [6, 14], (string[][])
            [
                ["Chuyên môn", Text(profile.Expertise)], ["Lĩnh vực nghiên cứu", Text(profile.ResearchFields)],
                ["Hướng nghiên cứu", Text(profile.ResearchDirections)], ["Tóm tắt hoạt động khoa học", Text(profile.ActivitySummary)]
            ]);
        Add("Thông tin kê khai và xác nhận", ["Xác nhận của cơ quan", "Người khai ký tên (ghi rõ chức danh, học vị)"], [10, 10], (string[][])
        [
            ["", $"{Text(d.DeclarationPlace)}, {Date(d.DeclarationDate)}\n{string.Join(" · ", new[] { l.AcademicRank, l.Degree, l.FullName }.Where(x => !string.IsNullOrWhiteSpace(x)))}"]
        ]);
        return sections;
    }

    public static byte[] Pdf(IReadOnlyList<Section> sections) => Document.Create(doc => doc.Page(page =>
    {
        page.Size(PageSizes.A4); page.Margin(35);
        // QuestPDF ships Lato with Vietnamese glyphs; embed it on Windows and Linux.
        page.DefaultTextStyle(s => s.FontFamily("Lato").FontSize(10));
        page.Header().AlignCenter().Text("LÝ LỊCH KHOA HỌC").Bold().FontSize(16);
        page.Content().PaddingTop(10).Column(col =>
        {
            foreach (var section in sections)
            {
                col.Item().PaddingTop(12).EnsureSpace(60).Column(block =>
                {
                    block.Item().PaddingBottom(6).Text(section.Title).Bold();
                    if (section.Rows.Count == 0) { block.Item().Text("Chưa có dữ liệu."); return; }
                    block.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns => { foreach (var width in section.Widths) columns.RelativeColumn(width); });
                        table.Header(header => { foreach (var text in section.Headers) header.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).Padding(5).Text(text).Bold(); });
                        foreach (var row in section.Rows)
                            foreach (var text in row) table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(text);
                    });
                });
            }
        });
        page.Footer().AlignCenter().Text(t => { t.Span("Trang "); t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
    })).GeneratePdf();

    public static byte[] Excel(IReadOnlyList<Section> sections)
    {
        using var book = new XLWorkbook();
        var sheet = book.Worksheets.Add("Ly lich khoa hoc");
        sheet.Style.Font.FontName = "Times New Roman"; sheet.Style.Font.FontSize = 11;
        sheet.Columns(1, 20).Width = 3.5;
        sheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        sheet.Style.Alignment.WrapText = true;
        sheet.Range(1, 1, 1, 20).Merge().Value = "LÝ LỊCH KHOA HỌC";
        sheet.Row(1).Height = 30; sheet.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(16);
        sheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        var rowIndex = 3;
        var pageHeight = 42d;
        Section? activeSection = null;
        foreach (var section in sections)
        {
            activeSection = section;
            var headingHeight = section.Title.Contains('\n') ? 40 : 25;
            var firstRowHeight = section.Rows.Count == 0 ? 22 : Math.Min(262, RowHeight(section.Rows[0], section.Widths));
            if (pageHeight + headingHeight + RowHeight(section.Headers, section.Widths) + firstRowHeight > 660)
                NewPage();
            sheet.Range(rowIndex, 1, rowIndex, 20).Merge().Value = section.Title;
            sheet.Cell(rowIndex, 1).Style.Font.SetBold();
            sheet.Row(rowIndex++).Height = headingHeight;
            pageHeight += headingHeight;
            WriteRow(section.Headers, section.Widths, true);
            if (section.Rows.Count == 0)
            {
                sheet.Range(rowIndex, 1, rowIndex, 20).Merge().Value = "Chưa có dữ liệu.";
                sheet.Row(rowIndex++).Height = 22;
                pageHeight += 22;
            }
            foreach (var values in section.Rows)
            {
                // Continue very long cells on extra rows instead of Excel's 409pt row-height clipping.
                var parts = values.Select((value, index) => Split(value, Math.Max(60, (section.Widths[index] * 5 - 2) * 18)).ToArray()).ToArray();
                for (var part = 0; part < parts.Max(p => p.Length); part++)
                    WriteRow(parts.Select(p => part < p.Length ? p[part] : "").ToArray(), section.Widths, false);
            }
            rowIndex++;
            pageHeight += 15;
        }
        sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        sheet.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.Margins.Top = sheet.PageSetup.Margins.Bottom = 0.6;
        sheet.PageSetup.Margins.Left = sheet.PageSetup.Margins.Right = 0.4;
        sheet.PageSetup.PrintAreas.Add(1, 1, rowIndex - 1, 20);
        sheet.PageSetup.SetRowsToRepeatAtTop(1, 1);
        sheet.PageSetup.Footer.Center.AddText("Trang &P / &N");
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();

        void WriteRow(string[] values, int[] widths, bool header)
        {
            var height = RowHeight(values, widths);
            if (pageHeight + height > 660)
            {
                NewPage();
                if (!header && activeSection is not null) WriteRow(activeSection.Headers, widths, true);
            }
            var column = 1;
            for (var i = 0; i < values.Length; i++)
            {
                var range = sheet.Range(rowIndex, column, rowIndex, column + widths[i] - 1).Merge();
                range.Value = values[i];
                range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.OutsideBorderColor = XLColor.LightGray;
                if (header) { range.Style.Font.SetBold(); range.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEEEEE"); }
                column += widths[i];
            }
            sheet.Row(rowIndex++).Height = height;
            pageHeight += height;
        }
        void NewPage()
        {
            sheet.PageSetup.AddHorizontalPageBreak(rowIndex - 1);
            pageHeight = 30; // repeated document title
        }
        static double RowHeight(string[] values, int[] widths) => Math.Min(409, values.Select((value, i) =>
            value.Split('\n').Sum(s => Math.Max(1, (int)Math.Ceiling(s.Length / (double)Math.Max(3, widths[i] * 5 - 2)))))
            .DefaultIfEmpty(1).Max() * 14 + 10);
    }

    private static IEnumerable<string> Split(string value, int size)
    {
        if (value.Length == 0) { yield return ""; yield break; }
        for (var offset = 0; offset < value.Length; offset += size)
            yield return value.Substring(offset, Math.Min(size, value.Length - offset));
    }
}
