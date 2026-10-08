using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.Products;
using ResearchManagement.Application.Products.Models;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Authorize]
public sealed class ProductsController(ProductService productService) : ControllerBase
{
    // GET /api/research-plan-items/{itemId}/products
    [HttpGet("api/research-plan-items/{itemId:long}/products")]
    public async Task<ActionResult<IReadOnlyList<ProductSummaryResponse>>> GetByPlanItem(
        long itemId, CancellationToken cancellationToken)
    {
        var result = await productService.GetByPlanItemIdAsync(itemId, cancellationToken);
        return Ok(result);
    }

    // POST /api/research-plan-items/{itemId}/products
    [HttpPost("api/research-plan-items/{itemId:long}/products")]
    public async Task<ActionResult<ProductResponse>> Create(
        long itemId,
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        request.ResearchPlanItemId = itemId;
        var result = await productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // GET /api/products/{id}
    [HttpGet("api/products/{id:long}")]
    public async Task<ActionResult<ProductResponse>> GetById(
        long id, CancellationToken cancellationToken)
    {
        var result = await productService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    // PUT /api/products/{id}
    [HttpPut("api/products/{id:long}")]
    public async Task<ActionResult<ProductResponse>> Update(
        long id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    // POST /api/products/{id}/submit
    [HttpPost("api/products/{id:long}/submit")]
    public async Task<ActionResult<ProductResponse>> Submit(
        long id, CancellationToken cancellationToken)
    {
        var result = await productService.SubmitAsync(id, cancellationToken);
        return Ok(result);
    }

    // POST /api/products/{id}/review
    [HttpPost("api/products/{id:long}/review")]
    public async Task<ActionResult<ProductResponse>> Review(
        long id,
        [FromBody] ReviewProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.ReviewAsync(id, request, cancellationToken);
        return Ok(result);
    }

    // GET /api/products/{id}/review-history
    [HttpGet("api/products/{id:long}/review-history")]
    public async Task<ActionResult<IReadOnlyList<ReviewHistoryResponse>>> GetReviewHistory(
        long id, CancellationToken cancellationToken)
    {
        var result = await productService.GetReviewHistoryAsync(id, cancellationToken);
        return Ok(result);
    }
}
