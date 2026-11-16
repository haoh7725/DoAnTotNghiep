using ClosedXML.Excel;
using QuestPDF.Infrastructure;
using ResearchManagement.Api.Infrastructure;
using ResearchManagement.Application.Lecturers.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Api.Tests;

public sealed class ScientificProfileExportTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(80)]
    public void Both_formats_preserve_profile_manual_entries_and_all_product_groups(int count)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var lecturer = new Lecturer(1, 1, "FIXTURE", "Giảng viên kiểm thử", null, "NU", null, null, null, "Tiến sĩ", null);
        var details = new ScientificProfileDetails
        {
            Education = [new() { Institution = "Trường kiểm thử", Specialization = "Công nghệ thông tin", GraduationYear = 2020 }],
            Employment = [new() { Organization = "Đơn vị kiểm thử", Position = "Giảng viên" }],
            ResearchProjects = [new() { Title = "Đề tài kê khai", Level = "Trường", Responsibility = "Thành viên", EndYear = 2027 }],
            Publications = [new() { Title = "Công bố kê khai", Journal = "Tạp chí kiểm thử", Year = 2024 }]
        };
        var codes = new[] { "DE_TAI", "BAI_BAO", "HOI_NGHI_HOI_THAO", "SACH" };
        var products = Enumerable.Range(0, count).Select(i => new ScientificProfileDocument.Product(
            $"Công trình kiểm thử {i}: " + new string('x', 250), codes[i % 4], codes[i % 4], new(2026, 11, 1), "Nơi công bố kiểm thử", "Trường", "TAC_GIA_CHINH")).ToList();
        var sections = ScientificProfileDocument.Build(lecturer, "Bộ môn kiểm thử", details, null, products);
        Assert.Equal(count, sections.Where(s => s.Title.Contains("đã công nhận")).Sum(s => s.Rows.Count));
        Assert.Contains(sections.SelectMany(s => s.Rows), r => r.Contains("Đề tài kê khai"));
        Assert.Contains(sections.SelectMany(s => s.Rows), r => r.Contains("Công bố kê khai"));
        Assert.Contains(sections.SelectMany(s => s.Rows), r => r.Contains("Trường kiểm thử\n—"));
        var xlsx = ScientificProfileDocument.Excel(sections);
        using var book = new XLWorkbook(new MemoryStream(xlsx));
        var sheet = book.Worksheet(1);
        Assert.Equal(XLPaperSize.A4Paper, sheet.PageSetup.PaperSize);
        Assert.Equal(1, sheet.PageSetup.PagesWide);
        Assert.Equal(0, sheet.PageSetup.PagesTall);
        Assert.Single(sheet.PageSetup.PrintAreas);
        var cells = sheet.CellsUsed().Select(c => c.GetString()).ToList();
        Assert.Contains("Giảng viên kiểm thử", cells);
        Assert.Contains("Công bố kê khai", cells);
        Assert.Equal(4, cells.Count(c => c.Contains("đã công nhận")));
        var pdf = ScientificProfileDocument.Pdf(sections);
        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        // Optional review fixtures never enter the database or production exports.
        if (Environment.GetEnvironmentVariable("PROFILE_EXPORT_EVIDENCE") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, $"fixture-{count}-products.xlsx"), xlsx);
            File.WriteAllBytes(Path.Combine(directory, $"fixture-{count}-products.pdf"), pdf);
        }
    }
}
