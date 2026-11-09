using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Api.Contracts;
using ResearchManagement.Application.Common;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Controllers;

[ApiController, Route("api/emulation-classification-rules"), Authorize]
public sealed class EmulationClassificationRulesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] long academicYearId, CancellationToken ct)
    {
        if (academicYearId <= 0) throw new BusinessRuleException("Vui lòng chọn năm học.");
        return Ok(await db.EmulationClassificationRules.AsNoTracking()
            .Where(x => x.AcademicYearId == academicYearId)
            .OrderByDescending(x => x.MinimumPercent).ThenBy(x => x.SortOrder)
            .Select(x => new { x.Id, x.AcademicYearId, x.Name, x.MinimumPercent, x.SortOrder }).ToListAsync(ct));
    }

    [HttpPost, Authorize(Policy = Policies.ResearchOffice)]
    public async Task<IActionResult> Create(EmulationClassificationRuleRequest request, CancellationToken ct)
    {
        Validate(request);
        if (!await db.AcademicYears.AnyAsync(x => x.Id == request.AcademicYearId, ct))
            throw new NotFoundException("Không tìm thấy năm học.");
        if (await db.EmulationClassificationRules.AnyAsync(x => x.AcademicYearId == request.AcademicYearId && (x.Name == request.Name.Trim() || x.MinimumPercent == request.MinimumPercent), ct))
            throw new ConflictException("Tên xếp loại hoặc ngưỡng tỷ lệ đã tồn tại trong năm học.");
        var rule = new EmulationClassificationRule(request.AcademicYearId, request.Name, request.MinimumPercent, request.SortOrder);
        db.EmulationClassificationRules.Add(rule); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { academicYearId = rule.AcademicYearId }, new { rule.Id, rule.AcademicYearId, rule.Name, rule.MinimumPercent, rule.SortOrder });
    }

    [HttpPut("{id:long}"), Authorize(Policy = Policies.ResearchOffice)]
    public async Task<IActionResult> Update(long id, EmulationClassificationRuleRequest request, CancellationToken ct)
    {
        Validate(request);
        var rule = await db.EmulationClassificationRules.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Không tìm thấy quy chế xếp loại.");
        if (!await db.AcademicYears.AnyAsync(x => x.Id == request.AcademicYearId, ct)) throw new NotFoundException("Không tìm thấy năm học.");
        if (await db.EmulationClassificationRules.AnyAsync(x => x.Id != id && x.AcademicYearId == request.AcademicYearId && (x.Name == request.Name.Trim() || x.MinimumPercent == request.MinimumPercent), ct))
            throw new ConflictException("Tên xếp loại hoặc ngưỡng tỷ lệ đã tồn tại trong năm học.");
        rule.Update(request.AcademicYearId, request.Name, request.MinimumPercent, request.SortOrder); await db.SaveChangesAsync(ct);
        return Ok(new { rule.Id, rule.AcademicYearId, rule.Name, rule.MinimumPercent, rule.SortOrder });
    }

    [HttpDelete("{id:long}"), Authorize(Policy = Policies.ResearchOffice)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var rule = await db.EmulationClassificationRules.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Không tìm thấy quy chế xếp loại.");
        db.EmulationClassificationRules.Remove(rule); await db.SaveChangesAsync(ct); return NoContent();
    }

    private static void Validate(EmulationClassificationRuleRequest request)
    {
        if (request.AcademicYearId <= 0 || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100 || request.MinimumPercent < 0 || request.SortOrder < 0)
            throw new BusinessRuleException("Dữ liệu quy chế xếp loại không hợp lệ.");
    }
}
