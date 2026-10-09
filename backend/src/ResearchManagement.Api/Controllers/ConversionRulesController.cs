using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Api.Contracts;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Controllers;

[ApiController, Route("api/admin/conversion-rules"), Authorize(Policy = Policies.Admin)]
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
        var rule = await db.ConversionRules.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (rule is null) return NotFound();
        if (!await db.ProductTypes.AnyAsync(x => x.Id == request.ProductTypeId, ct))
            return UnprocessableEntity(new { title = "Loại sản phẩm không tồn tại." });
        if (await db.ConversionRules.AnyAsync(x => x.Id != id && x.Code == request.Code.Trim() && x.Version == request.Version, ct))
            return Conflict(new { title = "Mã quy định và phiên bản đã tồn tại." });

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        rule.Update(request.ProductTypeId, request.Code, request.Version, request.Name, request.ConditionDescription,
            request.ConversionValue, request.Unit, request.AuthorRuleDescription, request.EffectiveFrom, request.EffectiveTo, request.LegalBasis);
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
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.ConversionCriteria.Where(x => x.ConversionRuleId == id).ExecuteDeleteAsync(ct);
        await db.AuthorConversionCoefficients.Where(x => x.ConversionRuleId == id).ExecuteDeleteAsync(ct);
        db.ConversionRules.Remove(rule); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return NoContent();
    }

    private static ConversionRule MakeRule(ConversionRuleRequest r) => new(r.ProductTypeId, r.Code, r.Version, r.Name,
        r.ConditionDescription, r.ConversionValue, r.Unit, r.AuthorRuleDescription, r.EffectiveFrom, r.EffectiveTo, r.LegalBasis);

    private void AddChildren(long id, ConversionRuleRequest r)
    {
        db.ConversionCriteria.AddRange(r.Criteria.Select(x => new ConversionCriterion(id, x.Field, x.Operator, x.StringValue, x.NumericValue)));
        db.AuthorConversionCoefficients.AddRange(r.AuthorCoefficients.Select(x => new AuthorConversionCoefficient(id, x.AuthorRole, x.Coefficient)));
    }

    private static string? Validate(ConversionRuleRequest r)
    {
        if (r.EffectiveTo < r.EffectiveFrom) return "Ngày hết hiệu lực không được trước ngày bắt đầu.";
        if (r.Criteria is null || r.AuthorCoefficients is null) return "Danh sách tiêu chí và hệ số tác giả là bắt buộc.";
        if (r.AuthorCoefficients.Select(x => x.AuthorRole.Trim().ToUpperInvariant()).Distinct().Count() != r.AuthorCoefficients.Count)
            return "Vai trò tác giả không được trùng.";
        foreach (var c in r.Criteria)
        {
            var textField = c.Field is "CHI_SO_TAP_CHI" or "PHAN_LOAI_TAP_CHI" or "CAP_DE_TAI";
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
        return rules.Select(x => (object)new { x.Id, x.ProductTypeId, x.Code, x.Version, x.Name, x.ConditionDescription,
            x.ConversionValue, x.Unit, x.AuthorRuleDescription, x.EffectiveFrom, x.EffectiveTo, x.LegalBasis,
            criteria = criteria.Where(c => c.ConversionRuleId == x.Id).Select(c => new { c.Id, c.Field, c.Operator, c.StringValue, c.NumericValue }),
            authorCoefficients = coefficients.Where(c => c.ConversionRuleId == x.Id).Select(c => new { c.AuthorRole, c.Coefficient }) }).ToList();
    }
}
