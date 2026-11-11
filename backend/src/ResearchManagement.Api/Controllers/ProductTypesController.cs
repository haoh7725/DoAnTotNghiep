using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Application.ProductTypes;
using ResearchManagement.Application.ProductTypes.Models;
using ResearchManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ResearchManagement.Api.Controllers;

/// <summary>Danh mục loại sản phẩm: đọc cho mọi tài khoản đăng nhập, ghi chỉ dành cho Quản trị.</summary>
[ApiController]
[Route("api/product-types")]
[Authorize]
public sealed class ProductTypesController(ProductTypeService productTypes, ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductTypeResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await productTypes.ListAsync(cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductTypeResponse>> GetById(long id, CancellationToken cancellationToken) =>
        Ok(await productTypes.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<ProductTypeResponse>> Create(
        [FromBody] CreateProductTypeRequest request, CancellationToken cancellationToken)
    {
        var result = await productTypes.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<ProductTypeResponse>> Update(
        long id, [FromBody] UpdateProductTypeRequest request, CancellationToken cancellationToken) =>
        Ok(await productTypes.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:long}")]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var item = await db.ProductTypes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();
        item.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/activate"), Authorize(Policy = Policies.ResearchOffice)]
    public async Task<IActionResult> Activate(long id, CancellationToken ct)
    {
        var item = await db.ProductTypes.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        item.IsActive = true; await db.SaveChangesAsync(ct); return NoContent();
    }
}
