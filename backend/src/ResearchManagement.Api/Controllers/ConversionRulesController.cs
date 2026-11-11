using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Api.Contracts;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Controllers;

[ApiController, Route("api/admin/conversion-rules"), Authorize(Policy = Policies.ResearchOffice)]
public sealed class ConversionRulesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var rules = await db.ConversionRules.AsNoTracking().OrderBy(x => x.Code).ThenByDescending(x => x.Version).ToListAsync(ct);
        return Ok(await Views(rules, ct));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var rule = await db.ConversionRules.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return rule is null ? NotFound() : Ok((await Views([rule], ct))[0]);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ConversionRuleRequest request, CancellationToken ct)
    {
        if (Validate(request) is { } error) return UnprocessableEntity(new { title = error });
        if (await ValidateClassification(request, ct) is { } classificationError) return UnprocessableEntity(new { title = classificationError });
        if (await HasDuplicateScope(request, null, ct)) return Conflict(new { title = "Đã có quy định cùng phân loại, đơn vị, điều kiện, ưu tiên và phiên bản trong thời gian hiệu lực này." });
        if (!await db.ProductTypes.AnyAsync(x => x.Id == request.ProductTypeId, ct))
            return UnprocessableEntity(new { title = "Loại sản phẩm không tồn tại." });
        if (await db.ConversionRules.AnyAsync(x => x.Code == request.Code.Trim() && x.Version == request.Version, ct))
            return Conflict(new { title = "Mã quy định và phiên bản đã tồn tại." });

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var rule = MakeRule(request); db.ConversionRules.Add(rule); await db.SaveChangesAsync(ct);
        AddChildren(rule.Id, request); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = rule.Id }, (await Views([rule], ct))[0]);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, ConversionRuleRequest request, CancellationToken ct)
    {
        if (Validate(request) is { } error) return UnprocessableEntity(new { title = error });
        if (await ValidateClassification(request, ct) is { } classificationError) return UnprocessableEntity(new { title = classificationError });
        if (await HasDuplicateScope(request, id, ct)) return Conflict(new { title = "Quy định bị trùng phạm vi áp dụng với quy định khác." });
        var rule = await db.ConversionRules.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (rule is null) return NotFound();
        if (await db.EvaluationConversionDetails.AnyAsync(x => x.ConversionRuleId == id, ct))
            return Conflict(new { title = "Quy định đã dùng để đánh giá. Hãy tạo phiên bản mới để bảo toàn căn cứ kết quả cũ." });
        if (!await db.ProductTypes.AnyAsync(x => x.Id == request.ProductTypeId, ct))
            return UnprocessableEntity(new { title = "Loại sản phẩm không tồn tại." });
        if (await db.ConversionRules.AnyAsync(x => x.Id != id && x.Code == request.Code.Trim() && x.Version == request.Version, ct))
            return Conflict(new { title = "Mã quy định và phiên bản đã tồn tại." });

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        rule.Update(request.ProductTypeId, request.Code, request.Version, request.Name, request.ConditionDescription,
            request.ConversionValue, request.Unit, request.Priority, request.AuthorRuleDescription, request.EffectiveFrom, request.EffectiveTo, request.LegalBasis);
        rule.SetAuthorSharingMode(request.AuthorSharingMode);
        rule.ClassificationId = request.ClassificationId;
        await db.ConversionCriteria.Where(x => x.ConversionRuleId == id).ExecuteDeleteAsync(ct);
        await db.AuthorConversionCoefficients.Where(x => x.ConversionRuleId == id).ExecuteDeleteAsync(ct);
        AddChildren(id, request); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Ok((await Views([rule], ct))[0]);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var rule = await db.ConversionRules.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (rule is null) return NotFound();
        if (await db.EvaluationConversionDetails.AnyAsync(x => x.ConversionRuleId == id, ct))
            return Conflict(new { title = "Không được xóa quy định đã dùng để đánh giá." });
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.ConversionCriteria.Where(x => x.ConversionRuleId == id).ExecuteDeleteAsync(ct);
        await db.AuthorConversionCoefficients.Where(x => x.ConversionRuleId == id).ExecuteDeleteAsync(ct);
        db.ConversionRules.Remove(rule); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return NoContent();
    }

    private static ConversionRule MakeRule(ConversionRuleRequest r)
    {
        var rule = new ConversionRule(r.ProductTypeId, r.Code, r.Version, r.Name,
            r.ConditionDescription, r.ConversionValue, r.Unit, r.Priority, r.AuthorRuleDescription, r.EffectiveFrom, r.EffectiveTo, r.LegalBasis);
        rule.SetAuthorSharingMode(r.AuthorSharingMode);
        rule.ClassificationId = r.ClassificationId;
        return rule;
    }

    private void AddChildren(long id, ConversionRuleRequest r)
    {
        db.ConversionCriteria.AddRange(r.Criteria.Select(x => new ConversionCriterion(id, x.Field, x.Operator, x.StringValue, x.NumericValue)));
        db.AuthorConversionCoefficients.AddRange(r.AuthorCoefficients.Select(x => new AuthorConversionCoefficient(id, x.AuthorRole, x.Coefficient)));
    }

    private static string? Validate(ConversionRuleRequest r)
    {
        if (r.EffectiveFrom == default) return "Vui lòng nhập ngày bắt đầu hiệu lực.";
        if (r.AuthorSharingMode is not ("HE_SO" or "CHIA_136")) return "Cách chia tác giả không hợp lệ.";
        if (r.EffectiveTo < r.EffectiveFrom) return "Ngày hết hiệu lực không được trước ngày bắt đầu.";
        if (r.Criteria is null || r.AuthorCoefficients is null) return "Danh sách tiêu chí và hệ số tác giả là bắt buộc.";
        if (r.AuthorSharingMode == "HE_SO" && !r.AuthorCoefficients.Any(x => x.AuthorRole == "TAC_GIA_CHINH"))
            return "Cách chia theo hệ số phải cấu hình ít nhất vai trò tác giả chính.";
        if (r.AuthorCoefficients.Select(x => x.AuthorRole.Trim().ToUpperInvariant()).Distinct().Count() != r.AuthorCoefficients.Count)
            return "Vai trò tác giả không được trùng.";
        foreach (var c in r.Criteria)
        {
            var textField = c.Field is "CHI_SO_TAP_CHI" or "PHAN_LOAI_TAP_CHI" or "CAP_DE_TAI" or "HINH_THUC_CONG_BO";
            if (c.Field == "HINH_THUC_CONG_BO" && c.StringValue is not ("TAP_CHI" or "HOI_NGHI"))
                return "Hình thức công bố phải là Tạp chí hoặc Hội nghị.";
            var numberField = c.Field is "DIEM_CONG_TRINH" or "NAM_CONG_BO";
            if (!textField && !numberField) return "Trường dữ liệu của tiêu chí không hợp lệ.";
            if (textField && (c.Operator is not ("BANG" or "KHAC") || string.IsNullOrWhiteSpace(c.StringValue) || c.NumericValue is not null))
                return "Tiêu chí dạng chữ phải dùng BẰNG/KHÁC và có giá trị chữ.";
            if (numberField && (c.Operator is not ("BANG" or "KHAC" or "TU" or "DEN") || c.NumericValue is null || c.StringValue is not null))
                return "Tiêu chí dạng số phải có giá trị số và toán tử hợp lệ.";
        }
        return null;
    }

    private async Task<List<object>> Views(List<ConversionRule> rules, CancellationToken ct)
    {
        var ids = rules.Select(x => x.Id).ToList();
        var criteria = await db.ConversionCriteria.AsNoTracking().Where(x => ids.Contains(x.ConversionRuleId)).OrderBy(x => x.Id).ToListAsync(ct);
        var coefficients = await db.AuthorConversionCoefficients.AsNoTracking().Where(x => ids.Contains(x.ConversionRuleId)).OrderBy(x => x.AuthorRole).ToListAsync(ct);
        var usedIds = await db.EvaluationConversionDetails.AsNoTracking().Where(x => ids.Contains(x.ConversionRuleId))
            .Select(x => x.ConversionRuleId).Distinct().ToListAsync(ct);
        return rules.Select(x => (object)new { x.Id, x.ProductTypeId, x.Code, x.Version, x.Name, x.ConditionDescription,
            x.ConversionValue, x.Unit, x.Priority, x.AuthorRuleDescription, x.AuthorSharingMode, x.ClassificationId, x.EffectiveFrom, x.EffectiveTo, x.LegalBasis,
            IsUsed = usedIds.Contains(x.Id),
            criteria = criteria.Where(c => c.ConversionRuleId == x.Id).Select(c => new { c.Id, c.Field, c.Operator, c.StringValue, c.NumericValue }),
            authorCoefficients = coefficients.Where(c => c.ConversionRuleId == x.Id).Select(c => new { c.AuthorRole, c.Coefficient }) }).ToList();
    }

    private async Task<string?> ValidateClassification(ConversionRuleRequest request, CancellationToken ct)
    {
        if (!await db.ProductTypes.AnyAsync(x => x.Id == request.ProductTypeId && x.IsActive, ct))
            return "Nhóm sản phẩm không tồn tại hoặc đã ngừng dùng.";
        if (request.ClassificationId.HasValue && !await db.ProductClassifications.AnyAsync(x =>
            x.Id == request.ClassificationId && x.ProductTypeId == request.ProductTypeId && x.IsActive, ct))
            return "Phân loại không thuộc nhóm hoặc đã ngừng dùng.";
        return null;
    }

    private async Task<bool> HasDuplicateScope(ConversionRuleRequest request, long? exceptId, CancellationToken ct)
    {
        var candidates = await db.ConversionRules.AsNoTracking().Where(x => x.Id != exceptId &&
            x.ProductTypeId == request.ProductTypeId && x.ClassificationId == request.ClassificationId &&
            x.Unit == request.Unit && x.Priority == request.Priority && x.Version == request.Version &&
            (!request.EffectiveTo.HasValue || x.EffectiveFrom <= request.EffectiveTo) &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo >= request.EffectiveFrom)).Select(x => x.Id).ToListAsync(ct);
        var criteria = await db.ConversionCriteria.AsNoTracking().Where(x => candidates.Contains(x.ConversionRuleId)).ToListAsync(ct);
        static string Key(string field, string op, string? text, decimal? value) =>
            $"{field}|{op}|{text?.Trim().ToUpperInvariant()}|{value?.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        var expected = request.Criteria.Select(c => Key(c.Field, c.Operator, c.StringValue, c.NumericValue)).Order().ToArray();
        return candidates.Any(id => criteria.Where(c => c.ConversionRuleId == id)
            .Select(c => Key(c.Field, c.Operator, c.StringValue, c.NumericValue)).Order().SequenceEqual(expected));
    }
}
