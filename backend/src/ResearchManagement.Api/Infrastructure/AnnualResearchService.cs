using Microsoft.EntityFrameworkCore;
using ResearchManagement.Infrastructure.Persistence;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Application.Common;

namespace ResearchManagement.Api.Infrastructure;

/// <summary>Live calculations shared by annual previews and finalized snapshots.</summary>
public sealed class AnnualResearchService(ApplicationDbContext db)
{
    public const decimal RequiredHours = 586m;
    public static DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow,
        OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh").DateTime);
    public sealed record Line(long ProductId, long TypeId, string TypeName, string Title, long? TargetId,
        long RuleId, string RuleCode, decimal BaseHours, decimal Coefficient, decimal Hours, string Basis);
    public sealed record Target(long Id, string TypeName, int Assigned, int Registered, int Approved,
        decimal Percent, bool Completed, decimal Hours);
    public sealed record Result(long LecturerId, string LecturerName, string LecturerCode, long AcademicYearId,
        List<Line> Products, List<Target> Targets, List<string> Issues)
    {
        public decimal TotalHours => Products.Sum(x => x.Hours);
        public int AssignedTargets => Targets.Count;
        public int CompletedTargets => Targets.Count(x => x.Completed);
        public string Classification => TotalHours >= RequiredHours ? "Hoàn thành" : "Chưa hoàn thành";
    }

    public async Task<Result> Calculate(long lecturerId, long yearId, CancellationToken ct)
    {
        var lecturer = await db.Lecturers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == lecturerId, ct)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");
        var year = await db.AcademicYears.AsNoTracking().SingleOrDefaultAsync(x => x.Id == yearId, ct)
            ?? throw new NotFoundException("Không tìm thấy năm học.");
        var products = await (from p in db.Products.AsNoTracking()
            join item in db.ResearchPlanItems on p.ResearchPlanItemId equals item.Id
            join type in db.ProductTypes on item.ProductTypeId equals type.Id
            where p.Status == "DA_DUYET" && p.PublishedDate >= year.StartDate && p.PublishedDate <= year.EndDate
                && type.Code != "CHUNG_NHAN" && (type.Code != "BAI_BAO" || p.ArticleStatus == "DA_XUAT_BAN")
                && (p.SubmittedByLecturerId == lecturerId || db.ProductCoAuthors.Any(a => a.ProductId == p.Id && a.LecturerId == lecturerId))
            orderby p.PublishedDate, p.Id
            select new { Product = p, Type = type }).ToListAsync(ct);
        var lines = new List<Line>();
        var issues = new List<string>();
        var missingDates = await (from p in db.Products.AsNoTracking() join i in db.ResearchPlanItems on p.ResearchPlanItemId equals i.Id
            join plan in db.ResearchPlans on i.ResearchPlanId equals plan.Id
            where p.Status == "DA_DUYET" && p.PublishedDate == null && plan.AcademicYearId == yearId &&
                (p.SubmittedByLecturerId == lecturerId || db.ProductCoAuthors.Any(a => a.ProductId == p.Id && a.LecturerId == lecturerId))
            select p.Title).ToListAsync(ct);
        issues.AddRange(missingDates.Select(title => $"Thiếu ngày công bố/nghiệm thu: {title}"));
        var today = Today();
        var seen = new HashSet<string>();
        var rules = await db.ConversionRules.AsNoTracking().Where(x => x.Unit == "GIO" && x.Code.StartsWith("QD2023_")).ToListAsync(ct);
        var criteria = await db.ConversionCriteria.AsNoTracking().ToListAsync(ct);
        decimal facultyConferenceHours = 0;
        foreach (var row in products)
        {
            var p = row.Product;
            if (p.PublishedDate > today) { issues.Add($"Ngày công nhận nằm trong tương lai: {p.Title}"); continue; }
            var key = p.WorkIdentifier ?? $"{row.Type.Id}:{p.PublishedDate}:{System.Text.RegularExpressions.Regex.Replace(p.Title.Trim(), @"\s+", " ").ToLowerInvariant()}";
            if (!seen.Add(key)) { issues.Add($"Công trình kê khai trùng: {p.Title}"); continue; }
            var candidates = rules.Where(r => r.ProductTypeId == row.Type.Id && r.EffectiveFrom <= p.PublishedDate &&
                (!r.EffectiveTo.HasValue || r.EffectiveTo >= p.PublishedDate) &&
                (r.ClassificationId == null || r.ClassificationId == p.ClassificationId) &&
                criteria.Where(c => c.ConversionRuleId == r.Id).All(c => Matches(c, p)))
                .OrderByDescending(r => r.ClassificationId.HasValue).ThenByDescending(r => r.Priority).ThenByDescending(r => r.Version).ToList();
            var rule = candidates.FirstOrDefault();
            if (rule is null) { issues.Add($"Chưa có quy định quy đổi phù hợp: {p.Title}"); continue; }
            if (candidates.Count(r => r.ClassificationId.HasValue == rule.ClassificationId.HasValue && r.Priority == rule.Priority && r.Version == rule.Version) > 1)
            { issues.Add($"Quy định quy đổi cùng ưu tiên bị trùng: {p.Title}"); continue; }
            var role = p.SubmittedByLecturerId == lecturerId ? "TAC_GIA_CHINH" :
                await db.ProductCoAuthors.Where(a => a.ProductId == p.Id && a.LecturerId == lecturerId).Select(a => a.AuthorRole).FirstAsync(ct);
            var coefficients = await db.AuthorConversionCoefficients.Where(x => x.ConversionRuleId == rule.Id).ToListAsync(ct);
            decimal? coefficient = coefficients.Count == 0 ? 1m : coefficients.FirstOrDefault(x => x.AuthorRole == role)?.Coefficient;
            if (rule.AuthorSharingMode == "CHIA_136")
                coefficient = ConversionRule.SharedCoefficient(p.SubmittedByLecturerId == lecturerId,
                    1 + p.ExternalAuthorCount + await db.ProductCoAuthors.CountAsync(a => a.ProductId == p.Id && a.LecturerId != p.SubmittedByLecturerId, ct));
            if (!coefficient.HasValue) { issues.Add($"Chưa có hệ số tác giả cho {p.Title}"); continue; }
            var quantity = p.RecognitionQuantity;
            if (quantity > 1 && rule.Code is "QD2023_DT_NHA_NUOC" or "QD2023_DT_BO_TINH" or "QD2023_DT_NHOM_TRUONG")
            { issues.Add($"Số năm thực hiện trong một năm học không được vượt 1: {p.Title}"); continue; }
            if (quantity != 1 && rule.Code is not ("QD2023_DT_NHA_NUOC" or "QD2023_DT_BO_TINH" or "QD2023_DT_NHOM_TRUONG" or "QD2023_SACH_DICH"))
            { issues.Add($"Sản phẩm phải tính một đơn vị/công trình: {p.Title}"); continue; }
            var baseHours = rule.ConversionValue * quantity;
            var hours = decimal.Round(baseHours * coefficient.Value, 4);
            var basis = $"{rule.Code} v{rule.Version}; {rule.ConversionValue:0.####} giờ × {quantity:0.####} đơn vị × hệ số {coefficient:0.######}";
            if (rule.Code == "QD2023_HN_CAP_KHOA")
            {
                hours = Math.Min(hours, Math.Max(0, 120 - facultyConferenceHours));
                facultyConferenceHours += hours;
                basis += "; giới hạn hội thảo cấp khoa 120 giờ/giảng viên/năm học";
            }
            lines.Add(new(p.Id, row.Type.Id, row.Type.Name, p.Title, p.ResearchTargetId,
                rule.Id, rule.Code, baseHours, coefficient.Value, hours, basis));
        }
        var assignments = await (from a in db.LecturerTargetAllocations.AsNoTracking()
            join b in db.ResearchTargetAllocations on a.DepartmentAllocationId equals b.Id
            join t in db.ResearchTargets on b.ResearchTargetId equals t.Id
            join type in db.ProductTypes on t.ProductTypeId equals type.Id
            where a.LecturerId == lecturerId && t.AcademicYearId == yearId && t.Status == "DA_KHOA"
            select new { t.Id, TypeName = type.Name, TypeCode = type.Code, a.AllocatedArticleCount }).ToListAsync(ct);
        var targets = new List<Target>();
        foreach (var a in assignments)
        {
            var linked = await db.Products.AsNoTracking().Where(p => p.ResearchTargetId == a.Id && p.SubmittedByLecturerId == lecturerId).ToListAsync(ct);
            var approved = linked.Count(p => p.Status == "DA_DUYET" && p.PublishedDate.HasValue && p.PublishedDate <= today && (a.TypeCode != "BAI_BAO" || p.ArticleStatus == "DA_XUAT_BAN"));
            targets.Add(new(a.Id, a.TypeName, a.AllocatedArticleCount, linked.Count, approved,
                a.AllocatedArticleCount <= 0 ? 0 : decimal.Round(approved * 100m / a.AllocatedArticleCount, 2),
                approved >= a.AllocatedArticleCount, lines.Where(x => x.TargetId == a.Id).Sum(x => x.Hours)));
        }
        return new(lecturerId, lecturer.FullName, lecturer.Code, yearId, lines.OrderByDescending(x => x.Hours).ToList(), targets, issues);
    }

    private static bool Matches(ConversionCriterion c, Product p)
    {
        if (c.Field is "NAM_CONG_BO" or "DIEM_CONG_TRINH")
        {
            decimal? actual = c.Field == "NAM_CONG_BO" ? p.PublishedDate?.Year : p.ScoreEquivalent;
            return actual.HasValue && c.NumericValue.HasValue && c.Operator switch {
                "BANG" => actual == c.NumericValue, "TU" => actual >= c.NumericValue,
                "DEN" => actual <= c.NumericValue, "KHAC" => actual != c.NumericValue, _ => false };
        }
        var text = c.Field switch { "CHI_SO_TAP_CHI" => p.JournalIndex, "PHAN_LOAI_TAP_CHI" => p.JournalClassification, "CAP_DE_TAI" => p.ProjectLevel, _ => null };
        if (text is null) return false;
        var equal = string.Equals(text.Trim(), c.StringValue?.Trim(), StringComparison.OrdinalIgnoreCase);
        return c.Operator == "BANG" ? equal : c.Operator == "KHAC" && !equal;
    }
}
