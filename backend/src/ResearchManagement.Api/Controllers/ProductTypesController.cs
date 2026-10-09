using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Application.ProductTypes;
using ResearchManagement.Application.ProductTypes.Models;

namespace ResearchManagement.Api.Controllers;

/// <summary>Danh mục loại sản phẩm: đọc cho mọi tài khoản đăng nhập, ghi chỉ dành cho Quản trị.</summary>
[ApiController]
[Route("api/product-types")]
[Authorize]
public sealed class ProductTypesController(ProductTypeService productTypes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductTypeResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await productTypes.ListAsync(cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductTypeResponse>> GetById(long id, CancellationToken cancellationToken) =>
        Ok(await productTypes.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<ProductTypeResponse>> Create(
        [FromBody] CreateProductTypeRequest request, CancellationToken cancellationToken)
    {
        var result = await productTypes.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<ProductTypeResponse>> Update(
        long id, [FromBody] UpdateProductTypeRequest request, CancellationToken cancellationToken) =>
        Ok(await productTypes.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:long}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await productTypes.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
