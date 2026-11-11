using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Controllers;

[ApiController, Route("api/product-classifications"), Authorize]
public sealed class ProductClassificationsController(ApplicationDbContext db) : ControllerBase
{
    public sealed record SaveRequest(long ProductTypeId,
        [Required, RegularExpression("^[A-Z][A-Z0-9_]{0,49}$")] string Code,
        [Required, StringLength(300)] string Name, bool IsActive = true);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await db.ProductClassifications
        .AsNoTracking().OrderBy(x => x.ProductTypeId).ThenBy(x => x.Name).ToListAsync(ct));

    [HttpPost, Authorize(Policy = Policies.ResearchOffice)]
    public async Task<IActionResult> Create(SaveRequest request, CancellationToken ct)
    {
        if (!await db.ProductTypes.AnyAsync(x => x.Id == request.ProductTypeId && x.IsActive, ct))
            return UnprocessableEntity(new { title = "Nhóm không tồn tại hoặc đã ngừng dùng." });
        if (await db.ProductClassifications.AnyAsync(x => x.ProductTypeId == request.ProductTypeId && x.Code == request.Code, ct))
            return Conflict(new { title = "Mã phân loại đã tồn tại trong nhóm." });
        var item = new ProductClassification { ProductTypeId = request.ProductTypeId, Code = request.Code,
            Name = request.Name.Trim(), IsActive = request.IsActive };
        db.ProductClassifications.Add(item); await db.SaveChangesAsync(ct);
        return Created($"/api/product-classifications/{item.Id}", item);
    }

    [HttpPut("{id:long}"), Authorize(Policy = Policies.ResearchOffice)]
    public async Task<IActionResult> Update(long id, SaveRequest request, CancellationToken ct)
    {
        var item = await db.ProductClassifications.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (request.ProductTypeId != item.ProductTypeId || request.Code != item.Code)
            return Conflict(new { title = "Không được đổi nhóm hoặc mã của phân loại. Hãy tạo phân loại mới." });
        item.Name = request.Name.Trim(); item.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct); return Ok(item);
    }
}
