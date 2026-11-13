using ResearchManagement.Application.Evaluations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Api.Contracts;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Evaluations.Models;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;
using ResearchManagement.Api.Infrastructure;
using System.Globalization;
using System.Text;

namespace ResearchManagement.Api.Controllers;

[ApiController, Route("api/evaluations"), Authorize]
public sealed class EvaluationsController(
    ApplicationDbContext db,
    ICurrentUser currentUser,
    WorkflowNotificationDispatcher workflowNotifications) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<EvaluationDetailResponse>> Mine([FromQuery] long academicYearId, CancellationToken ct)
    {
        var lecturerId = await db.Lecturers.Where(x => x.AccountId == UserId()).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Tài khoản chưa liên kết với giảng viên.");
        var id = await db.Evaluations.AsNoTracking()
            .Where(x => x.LecturerId == lecturerId && x.AcademicYearId == academicYearId && x.Status == "CHOT")
            .OrderByDescending(x => x.Attempt).Select(x => (long?)x.Id).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Chưa có kết quả đánh giá đã chốt.");
        return await Detail(id, ct);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<EvaluationDetailResponse>> Detail(long id, CancellationToken ct)
    {
        var evaluation = await db.Evaluations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy kết quả đánh giá.");
        await EnsureCanView(evaluation, ct);
        return await ToDetail(evaluation, ct);
    }

    [HttpGet, Authorize(Policy = Policies.Management)]
    public async Task<ActionResult<IReadOnlyList<EvaluationSummaryResponse>>> List(
        [FromQuery] long academicYearId, [FromQuery] long? lecturerId, CancellationToken ct)
    {
        var query = db.Evaluations.AsNoTracking().Where(x => x.AcademicYearId == academicYearId);
        if (lecturerId.HasValue) query = query.Where(x => x.LecturerId == lecturerId.Value);
        var rows = await query.OrderByDescending(x => x.EvaluatedAt).ToListAsync(ct);
        var visible = new List<EvaluationSummaryResponse>();
        foreach (var row in rows)
        {
            if (!await CanManage(row.LecturerId, ct)) continue;
            if (row.Status == "NHAP" && row.EvaluatorAccountId != UserId() && !currentUser.IsInRole(Roles.Admin)) continue;
            visible.Add(await ToSummary(row, ct));
        }
        return visible;
    }

    [HttpPost, Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<EvaluationDetailResponse>> Create(CreateEvaluationRequest request, CancellationToken ct)
    {
        if (request.Unit is not ("GIO" or "DIEM")) throw new BusinessRuleException("Vui lòng chọn Giờ NCKH hoặc Điểm NCKH.");
        var lecturer = await db.Lecturers.SingleOrDefaultAsync(x => x.Id == request.LecturerId, ct)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");
        var year = await db.AcademicYears.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.AcademicYearId, ct)
            ?? throw new NotFoundException("Không tìm thấy năm học.");
        if (!await CanManage(lecturer.Id, ct)) throw new ForbiddenException("Không có quyền đánh giá giảng viên này.");

        var attempt = (await db.Evaluations.Where(x => x.LecturerId == lecturer.Id && x.AcademicYearId == request.AcademicYearId)
            .MaxAsync(x => (int?)x.Attempt, ct) ?? 0) + 1;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var evaluation = new Evaluation(lecturer.Id, request.AcademicYearId, attempt, UserId(), lecturer.Code,
            lecturer.FullName, lecturer.DepartmentId, lecturer.AcademicRank, lecturer.Degree, request.Unit);
        db.Evaluations.Add(evaluation);
        await db.SaveChangesAsync(ct);

        var missingDate = await (from p in db.Products.AsNoTracking()
            join item in db.ResearchPlanItems.AsNoTracking() on p.ResearchPlanItemId equals item.Id
            join plan in db.ResearchPlans.AsNoTracking() on item.ResearchPlanId equals plan.Id
            join productType in db.ProductTypes.AsNoTracking() on item.ProductTypeId equals productType.Id
            where productType.Code != "CHUNG_NHAN" && plan.AcademicYearId == request.AcademicYearId && p.Status == ProductStatuses.Approved && !p.PublishedDate.HasValue &&
                (p.SubmittedByLecturerId == lecturer.Id || db.ProductCoAuthors.Any(a => a.ProductId == p.Id && a.LecturerId == lecturer.Id))
            select p.Title).FirstOrDefaultAsync(ct);
        if (missingDate is not null)
            throw new BusinessRuleException($"Sản phẩm '{missingDate}' thiếu ngày công bố/công nhận để xác định hiệu lực quy định. Vui lòng bổ sung trước khi đánh giá.");

        var products = await (from p in db.Products.AsNoTracking()
                              join item in db.ResearchPlanItems.AsNoTracking() on p.ResearchPlanItemId equals item.Id
                              join plan in db.ResearchPlans.AsNoTracking() on item.ResearchPlanId equals plan.Id
                              join type in db.ProductTypes.AsNoTracking() on item.ProductTypeId equals type.Id
                              where p.PublishedDate >= year.StartDate && p.PublishedDate <= year.EndDate && p.Status == ProductStatuses.Approved &&
                                  (p.SubmittedByLecturerId == lecturer.Id || db.ProductCoAuthors.Any(a => a.ProductId == p.Id && a.LecturerId == lecturer.Id))
                              select new { Product = p, ProductTypeId = type.Id, ProductTypeName = type.Name, ProductTypeCode = type.Code,
                                  AuthorRole = p.SubmittedByLecturerId == lecturer.Id ? "TAC_GIA_CHINH" :
                                      db.ProductCoAuthors.Where(a => a.ProductId == p.Id && a.LecturerId == lecturer.Id)
                                          .Select(a => a.AuthorRole).FirstOrDefault() }).ToListAsync(ct);

        var duplicate = products.GroupBy(x => x.Product.WorkIdentifier is not null
            ? "ID:" + x.Product.WorkIdentifier
            : $"{x.ProductTypeId}:{x.Product.PublishedDate}:{System.Text.RegularExpressions.Regex.Replace(x.Product.Title.Trim(), @"\s+", " ").ToLowerInvariant()}")
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new BusinessRuleException("Có công trình được kê khai trùng trong kỳ đánh giá. Vui lòng hợp nhất hồ sơ trước khi quy đổi.");

        foreach (var product in products)
        {
            var date = product.Product.PublishedDate!.Value;
            var rules = await db.ConversionRules.AsNoTracking()
                .Where(x => x.ProductTypeId == product.ProductTypeId &&
                    (x.ClassificationId == product.Product.ClassificationId || x.ClassificationId == null) &&
                    x.Unit == request.Unit && x.EffectiveFrom <= date &&
                    (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= date))
                .OrderByDescending(x => x.Version).ToListAsync(ct);
            var criteria = await db.ConversionCriteria.AsNoTracking()
                .Where(x => rules.Select(r => r.Id).Contains(x.ConversionRuleId)).ToListAsync(ct);
            var publicationKind = product.Product.JournalConferenceId.HasValue
                ? await db.JournalConferences.Where(x => x.Id == product.Product.JournalConferenceId).Select(x => x.Type).SingleOrDefaultAsync(ct)
                : null;
            var matched = rules.Where(r => criteria.Where(c => c.ConversionRuleId == r.Id).All(c =>
                    c.Field == "HINH_THUC_CONG_BO" ? Compare(publicationKind, c.StringValue, c.Operator) : Matches(c, product.Product)))
                .GroupBy(r => r.Unit)
                .Select(group =>
                {
                    var candidates = group.Any(x => x.ClassificationId.HasValue) ? group.Where(x => x.ClassificationId.HasValue) : group;
                    var selected = candidates.OrderByDescending(x => x.Priority).ThenByDescending(x => x.Version).ThenBy(x => x.Id).First();
                    if (candidates.Count(x => x.Priority == selected.Priority && x.Version == selected.Version) > 1)
                        throw new BusinessRuleException($"Có nhiều quy định cùng ưu tiên cho đơn vị {group.Key}. Vui lòng cấu hình lại độ ưu tiên.");
                    return selected;
                }).ToList();
            if (matched.Count == 0 && product.ProductTypeCode == "CHUNG_NHAN") continue;
            if (matched.Count == 0)
                throw new BusinessRuleException($"Sản phẩm '{product.Product.Title}' chưa có quy định quy đổi phù hợp và còn hiệu lực. Vui lòng cấu hình trước khi đánh giá.");
            foreach (var rule in matched)
            {
                var role = NormalizeAuthorRole(product.AuthorRole ?? "DONG_TAC_GIA");
                var coefficients = await db.AuthorConversionCoefficients.AsNoTracking()
                    .Where(x => x.ConversionRuleId == rule.Id).ToListAsync(ct);
                decimal? coefficient = coefficients.Count == 0 ? 1m :
                    coefficients.FirstOrDefault(x => NormalizeAuthorRole(x.AuthorRole) == role)?.Coefficient;
                var totalAuthors = 1 + product.Product.ExternalAuthorCount + await db.ProductCoAuthors
                    .Where(x => x.ProductId == product.Product.Id && x.LecturerId != product.Product.SubmittedByLecturerId)
                    .Select(x => x.LecturerId).Distinct().CountAsync(ct);
                if (rule.AuthorSharingMode == "CHIA_136")
                    coefficient = ConversionRule.SharedCoefficient(product.Product.SubmittedByLecturerId == lecturer.Id, totalAuthors);
                if (!coefficient.HasValue)
                    throw new BusinessRuleException(
                        $"Quy định {rule.Code} chưa cấu hình hệ số cho vai trò tác giả '{role}' của sản phẩm '{product.Product.Title}'.");
                db.EvaluationConversionDetails.Add(new EvaluationConversionDetail(evaluation.Id, lecturer.Id,
                    request.AcademicYearId, product.Product.Id, product.ProductTypeId, rule.Id, rule.ConversionValue,
                    coefficient.Value, rule.Unit,
                    $"Quy định {rule.Code} phiên bản {rule.Version}; {role}; " +
                    (rule.AuthorSharingMode == "CHIA_136" ? $"chia 1/3 cho tác giả chính và 2/3 cho {totalAuthors} tác giả (gồm người ngoài trường); " : "") +
                    $"hệ số {coefficient.Value:0.######}; căn cứ {rule.LegalBasis}"));
            }
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return CreatedAtAction(nameof(Detail), new { id = evaluation.Id }, await ToDetail(evaluation, ct));
    }

    private static string NormalizeAuthorRole(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.IsLetterOrDigit(character) ? char.ToUpperInvariant(character) : '_');
        var normalized = string.Join('_', builder.ToString().Split('_', StringSplitOptions.RemoveEmptyEntries));
        return normalized switch
        {
            "DONG_TAC_GIA" => "DONG_TAC_GIA",
            "TAC_GIA_LIEN_HE" => "TAC_GIA_LIEN_HE",
            "TAC_GIA_CHINH" => "TAC_GIA_CHINH",
            _ => normalized
        };
    }

    [HttpPost("{id:long}/finalize"), Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<EvaluationDetailResponse>> Finalize(long id, FinalizeEvaluationRequest request, CancellationToken ct)
    {
        var evaluation = await db.Evaluations.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy kết quả đánh giá.");
        if (evaluation.EvaluatorAccountId != UserId() && !currentUser.IsInRole(Roles.Admin))
            throw new ForbiddenException("Chỉ người đánh giá hoặc quản trị viên được chốt kết quả.");
        var unit = evaluation.EvaluationUnit == "LEGACY" ? request.Unit : evaluation.EvaluationUnit;
        if (unit is not ("GIO" or "DIEM"))
            throw new BusinessRuleException("Vui lòng chọn Giờ NCKH hoặc Điểm NCKH trước khi chốt bản nháp cũ.");
        if (evaluation.EvaluationUnit != "LEGACY" && request.Unit is not null && request.Unit != unit)
            throw new BusinessRuleException("Phương thức đã chọn khi tạo đánh giá. Hãy tạo lần đánh giá mới nếu cần đổi phương thức.");
        var classification = await CalculateClassificationAsync(evaluation, unit, ct);
        if (evaluation.Status != "CHOT") evaluation.SelectUnit(unit);
        try { evaluation.Finalize(classification.Name, classification.Basis); }
        catch (InvalidOperationException e) { throw new ConflictException(e.Message); }
        var resultOwner = await db.Lecturers.AsNoTracking()
            .Where(x => x.Id == evaluation.LecturerId)
            .Select(x => new { x.AccountId, x.FullName })
            .SingleAsync(ct);
        var yearCode = await db.AcademicYears.AsNoTracking()
            .Where(x => x.Id == evaluation.AcademicYearId)
            .Select(x => x.Code)
            .SingleAsync(ct);
        await workflowNotifications.QueueForAccountAsync(
            resultOwner.AccountId,
            $"Kết quả thi đua năm học {yearCode} của bạn đã được chốt: {classification.Name}.",
            $"EVALUATION_FINALIZED:{evaluation.Id}",
            "evaluations",
            null,
            ct);
        await db.SaveChangesAsync(ct);
        return await ToDetail(evaluation, ct);
    }

    private static bool Matches(ConversionCriterion criterion, Product product)
    {
        if (criterion.Field == "NAM_CONG_BO")
            return Compare(product.PublishedDate?.Year, criterion.NumericValue, criterion.Operator);
        if (criterion.Field == "DIEM_CONG_TRINH")
            return Compare(product.ScoreEquivalent, criterion.NumericValue, criterion.Operator);
        var actual = criterion.Field switch
        {
            "CHI_SO_TAP_CHI" => product.JournalIndex,
            "PHAN_LOAI_TAP_CHI" => product.JournalClassification,
            "CAP_DE_TAI" => product.ProjectLevel,
            _ => null
        };
        return Compare(actual, criterion.StringValue, criterion.Operator);
    }

    private static bool Compare(decimal? actual, decimal? expected, string op) => actual.HasValue && expected.HasValue && op switch
    {
        "BANG" => actual.Value == expected.Value,
        "KHAC" => actual.Value != expected.Value,
        "TU" => actual.Value >= expected.Value,
        "DEN" => actual.Value <= expected.Value,
        _ => false
    };

    private static bool Compare(string? actual, string? expected, string op)
    {
        if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected)) return false;
        var equal = string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
        return op == "BANG" ? equal : op == "KHAC" && !equal;
    }

    private async Task EnsureCanView(Evaluation evaluation, CancellationToken ct)
    {
        var owner = await db.Lecturers.AnyAsync(x => x.Id == evaluation.LecturerId && x.AccountId == UserId(), ct);
        if (evaluation.Status == "NHAP" && evaluation.EvaluatorAccountId != UserId() && !currentUser.IsInRole(Roles.Admin))
            throw new ForbiddenException("Kết quả nháp chỉ người đánh giá và quản trị viên được xem.");
        if (!owner && !await CanManage(evaluation.LecturerId, ct))
            throw new ForbiddenException("Không có quyền xem kết quả này.");
    }

    private async Task<bool> CanManage(long lecturerId, CancellationToken ct)
    {
        var unit = await (from l in db.Lecturers.AsNoTracking() join d in db.Departments.AsNoTracking()
                          on l.DepartmentId equals d.Id where l.Id == lecturerId
                          select new { d.FacultyId, DepartmentId = d.Id }).SingleOrDefaultAsync(ct);
        return unit is not null && currentUser.CanAccess(unit.FacultyId, unit.DepartmentId);
    }

    private async Task<(string Name, string Basis)> CalculateClassificationAsync(Evaluation evaluation, string unit, CancellationToken ct)
    {
        var norm = await db.ResearchNorms.AsNoTracking()
            .Where(x => x.LecturerId == evaluation.LecturerId && x.AcademicYearId == evaluation.AcademicYearId)
            .Select(x => new { x.RequiredHours, x.RequiredPoints }).SingleOrDefaultAsync(ct);
        if (norm is null) throw new BusinessRuleException("Chưa có định mức NCKH cho năm học này.");
        var conversions = await db.EvaluationConversionDetails.AsNoTracking()
            .Where(x => x.EvaluationId == evaluation.Id).ToListAsync(ct);
        if (conversions.GroupBy(x => x.ProductId).Any(g => !g.Any(x => x.Unit == unit)))
            throw new BusinessRuleException("Thiếu quy đổi theo phương thức đã chọn. Hãy cấu hình và tạo lại đánh giá trước khi chốt.");
        var totals = await Totals(evaluation.Id, ct);
        var percent = EvaluationMetric.Percent(unit, totals.Hours, totals.Points, norm.RequiredHours, norm.RequiredPoints);
        var rules = await db.EmulationClassificationRules.AsNoTracking()
            .Where(x => x.AcademicYearId == evaluation.AcademicYearId)
            .OrderByDescending(x => x.MinimumPercent).ThenBy(x => x.SortOrder).ToListAsync(ct);
        if (rules.Count == 0)
            throw new BusinessRuleException("Chưa cấu hình quy chế xếp loại cho năm học này.");
        var rule = rules.FirstOrDefault(x => percent >= x.MinimumPercent);
        var name = rule?.Name ?? "Chưa đạt";
        var basis = rule is null
            ? "Không đạt ngưỡng thấp nhất của quy chế xếp loại."
            : $"Đạt ngưỡng {rule.Name} từ {rule.MinimumPercent:0.##}% theo quy chế năm học.";
        var calculation = unit == "DIEM"
            ? $"điểm NCKH: {totals.Points:0.####}/{norm.RequiredPoints:0.####} điểm"
            : $"giờ NCKH: {totals.Hours:0.####}/{norm.RequiredHours:0.####} giờ";
        return (name, $"Đánh giá theo {(unit == "GIO" ? "giờ NCKH" : "điểm NCKH")}; căn cứ {calculation} ({percent:0.##}%). {basis}");
    }

    private async Task<EvaluationSummaryResponse> ToSummary(Evaluation e, CancellationToken ct)
    {
        var year = await db.AcademicYears.AsNoTracking().Where(x => x.Id == e.AcademicYearId).Select(x => x.Code).SingleAsync(ct);
        var totals = await Totals(e.Id, ct);
        return new(e.Id, e.LecturerId, e.AcademicYearId, year, e.Attempt, e.Status, e.Classification, totals.Hours, totals.Points, e.EvaluatedAt, e.EvaluationUnit);
    }

    private async Task<EvaluationDetailResponse> ToDetail(Evaluation e, CancellationToken ct)
    {
        var summary = await ToSummary(e, ct);
        var details = await (from d in db.EvaluationConversionDetails.AsNoTracking()
                             join p in db.Products.AsNoTracking() on d.ProductId equals p.Id
                             join t in db.ProductTypes.AsNoTracking() on d.ProductTypeId equals t.Id
                             join r in db.ConversionRules.AsNoTracking() on d.ConversionRuleId equals r.Id
                             where d.EvaluationId == e.Id orderby d.Id
                             select new ConversionDetailResponse(d.Id, d.ProductId, p.Title, d.ProductTypeId, t.Name,
                                 d.ConversionRuleId, r.Code, r.Version, d.BaseValue, d.AuthorCoefficient,
                                 d.ConvertedValue, d.Unit, d.CalculationBasis)).ToListAsync(ct);
        return new(e.Id, e.LecturerId, e.AcademicYearId, summary.AcademicYearCode, e.Attempt, e.Status,
            e.Classification, e.ClassificationBasis, e.LecturerCodeSnapshot, e.LecturerNameSnapshot,
            e.DepartmentIdSnapshot, e.AcademicRankSnapshot, e.DegreeSnapshot, summary.TotalHours,
            summary.TotalPoints, e.EvaluatedAt, details, e.EvaluationUnit);
    }

    private async Task<(decimal Hours, decimal Points)> Totals(long id, CancellationToken ct)
    {
        var rows = await db.EvaluationConversionDetails.AsNoTracking().Where(x => x.EvaluationId == id)
            .GroupBy(_ => 1).Select(g => new { Hours = g.Sum(x => x.Unit == "GIO" ? x.ConvertedValue : 0),
                Points = g.Sum(x => x.Unit == "DIEM" ? x.ConvertedValue : 0) }).SingleOrDefaultAsync(ct);
        return (rows?.Hours ?? 0, rows?.Points ?? 0);
    }

    private long UserId() => currentUser.Id ?? throw new ForbiddenException("Không xác định được tài khoản hiện tại.");
}
