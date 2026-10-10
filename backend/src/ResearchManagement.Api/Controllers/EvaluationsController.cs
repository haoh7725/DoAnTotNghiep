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

namespace ResearchManagement.Api.Controllers;

[ApiController, Route("api/evaluations"), Authorize]
public sealed class EvaluationsController(ApplicationDbContext db, ICurrentUser currentUser) : ControllerBase
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
        var lecturer = await db.Lecturers.SingleOrDefaultAsync(x => x.Id == request.LecturerId, ct)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");
        if (!await db.AcademicYears.AnyAsync(x => x.Id == request.AcademicYearId, ct))
            throw new NotFoundException("Không tìm thấy năm học.");
        if (!await CanManage(lecturer.Id, ct)) throw new ForbiddenException("Không có quyền đánh giá giảng viên này.");

        var attempt = (await db.Evaluations.Where(x => x.LecturerId == lecturer.Id && x.AcademicYearId == request.AcademicYearId)
            .MaxAsync(x => (int?)x.Attempt, ct) ?? 0) + 1;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var evaluation = new Evaluation(lecturer.Id, request.AcademicYearId, attempt, UserId(), lecturer.Code,
            lecturer.FullName, lecturer.DepartmentId, lecturer.AcademicRank, lecturer.Degree);
        db.Evaluations.Add(evaluation);
        await db.SaveChangesAsync(ct);

        var products = await (from p in db.Products.AsNoTracking()
                              join item in db.ResearchPlanItems.AsNoTracking() on p.ResearchPlanItemId equals item.Id
                              join plan in db.ResearchPlans.AsNoTracking() on item.ResearchPlanId equals plan.Id
                              join type in db.ProductTypes.AsNoTracking() on item.ProductTypeId equals type.Id
                              where plan.AcademicYearId == request.AcademicYearId && p.Status == ProductStatuses.Approved &&
                                  (p.SubmittedByLecturerId == lecturer.Id || db.ProductCoAuthors.Any(a => a.ProductId == p.Id && a.LecturerId == lecturer.Id))
                              select new { Product = p, ProductTypeId = type.Id, ProductTypeName = type.Name,
                                  AuthorRole = p.SubmittedByLecturerId == lecturer.Id ? "TAC_GIA_CHINH" :
                                      db.ProductCoAuthors.Where(a => a.ProductId == p.Id && a.LecturerId == lecturer.Id)
                                          .Select(a => a.AuthorRole).FirstOrDefault() }).ToListAsync(ct);

        foreach (var product in products)
        {
            var date = product.Product.PublishedDate ?? DateOnly.FromDateTime(product.Product.UpdatedAt.UtcDateTime);
            var rules = await db.ConversionRules.AsNoTracking()
                .Where(x => x.ProductTypeId == product.ProductTypeId && x.EffectiveFrom <= date &&
                    (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= date))
                .OrderByDescending(x => x.Version).ToListAsync(ct);
            var criteria = await db.ConversionCriteria.AsNoTracking()
                .Where(x => rules.Select(r => r.Id).Contains(x.ConversionRuleId)).ToListAsync(ct);
            var matched = rules.Where(r => criteria.Where(c => c.ConversionRuleId == r.Id).All(c => Matches(c, product.Product)))
                .GroupBy(r => r.Unit).Select(g => g.First());
            foreach (var rule in matched)
            {
                var role = product.AuthorRole ?? "DONG_TAC_GIA";
                var coefficients = await db.AuthorConversionCoefficients.AsNoTracking()
                    .Where(x => x.ConversionRuleId == rule.Id).ToListAsync(ct);
                var coefficient = coefficients.Count == 0 ? 1m :
                    coefficients.FirstOrDefault(x => x.AuthorRole.ToUpper() == role)?.Coefficient;
                if (!coefficient.HasValue) continue;
                db.EvaluationConversionDetails.Add(new EvaluationConversionDetail(evaluation.Id, lecturer.Id,
                    request.AcademicYearId, product.Product.Id, product.ProductTypeId, rule.Id, rule.ConversionValue,
                    coefficient.Value, rule.Unit,
                    $"Quy định {rule.Code} phiên bản {rule.Version}; {role} hệ số {coefficient.Value:0.######}"));
            }
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return CreatedAtAction(nameof(Detail), new { id = evaluation.Id }, await ToDetail(evaluation, ct));
    }

    [HttpPost("{id:long}/finalize"), Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<EvaluationDetailResponse>> Finalize(long id, FinalizeEvaluationRequest request, CancellationToken ct)
    {
        var evaluation = await db.Evaluations.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy kết quả đánh giá.");
        if (evaluation.EvaluatorAccountId != UserId() && !currentUser.IsInRole(Roles.Admin))
            throw new ForbiddenException("Chỉ người đánh giá hoặc quản trị viên được chốt kết quả.");
        try { evaluation.Finalize(request.Classification, request.ClassificationBasis); }
        catch (InvalidOperationException e) { throw new ConflictException(e.Message); }
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

    private async Task<EvaluationSummaryResponse> ToSummary(Evaluation e, CancellationToken ct)
    {
        var year = await db.AcademicYears.AsNoTracking().Where(x => x.Id == e.AcademicYearId).Select(x => x.Code).SingleAsync(ct);
        var totals = await Totals(e.Id, ct);
        return new(e.Id, e.LecturerId, e.AcademicYearId, year, e.Attempt, e.Status, e.Classification, totals.Hours, totals.Points, e.EvaluatedAt);
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
            summary.TotalPoints, e.EvaluatedAt, details);
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
