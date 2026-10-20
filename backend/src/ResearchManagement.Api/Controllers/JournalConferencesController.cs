
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Controllers;

public sealed record JournalConferenceRequest(
    string? Name,
    string? Type,
    string? Issn,
    string? Index,
    string? Classification,
    string? Notes
);

[ApiController]
[Route("api/journal-conferences")]
[Authorize(Policy = Policies.Management)]
public sealed class JournalConferencesController(
    ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] string? type,
        CancellationToken ct)
    {
        var query = db.JournalConferences.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var search = keyword.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var selectedType = type.Trim().ToUpperInvariant();
            if (selectedType != "TAP_CHI" &&
                selectedType != "HOI_NGHI")
                return BadRequest(new { title = "Loại không hợp lệ." });

            query = query.Where(x => x.Type == selectedType);
        }

        var items = await query
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id, CancellationToken ct)
    {
        var item = await db.JournalConferences
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<IActionResult> Create(
        [FromBody] JournalConferenceRequest request,
        CancellationToken ct)
    {
        var error = Validate(request);
        if (error is not null)
            return BadRequest(new { title = error });

        var name = request.Name!.Trim();
        var type = request.Type!.Trim().ToUpperInvariant();

        var exists = await db.JournalConferences.AnyAsync(
            x => x.Type == type &&
                 x.Name.ToLower() == name.ToLower(), ct);

        if (exists)
            return Conflict(new { title = "Danh mục đã tồn tại." });

        var item = new JournalConference
        {
            Name = name,
            Type = type,
            Issn = Clean(request.Issn),
            Index = Clean(request.Index),
            Classification = Clean(request.Classification),
            Notes = Clean(request.Notes),
            CreatedAt = DateTime.UtcNow
        };

        db.JournalConferences.Add(item);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] JournalConferenceRequest request,
        CancellationToken ct)
    {
        var error = Validate(request);
        if (error is not null)
            return BadRequest(new { title = error });

        var item = await db.JournalConferences
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (item is null)
            return NotFound();

        var name = request.Name!.Trim();
        var type = request.Type!.Trim().ToUpperInvariant();

        var exists = await db.JournalConferences.AnyAsync(
            x => x.Id != id &&
                 x.Type == type &&
                 x.Name.ToLower() == name.ToLower(), ct);

        if (exists)
            return Conflict(new { title = "Danh mục đã tồn tại." });

        item.Name = name;
        item.Type = type;
        item.Issn = Clean(request.Issn);
        item.Index = Clean(request.Index);
        item.Classification = Clean(request.Classification);
        item.Notes = Clean(request.Notes);

        await db.SaveChangesAsync(ct);
        return Ok(item);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Validate(
        JournalConferenceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return "Tên tạp chí/hội nghị không được để trống.";

        if (request.Name.Trim().Length > 300)
            return "Tên không được vượt quá 300 ký tự.";

        var type = request.Type?.Trim().ToUpperInvariant();
        if (type != "TAP_CHI" && type != "HOI_NGHI")
            return "Loại phải là TAP_CHI hoặc HOI_NGHI.";

        if (Clean(request.Issn)?.Length > 30)
            return "ISSN không được vượt quá 30 ký tự.";

        if (Clean(request.Index)?.Length > 100)
            return "Chỉ số không được vượt quá 100 ký tự.";

        if (Clean(request.Classification)?.Length > 100)
            return "Phân loại không được vượt quá 100 ký tự.";

        return null;
    }
}
