using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.Products;
using ResearchManagement.Application.Products.Models;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products/{productId:long}/co-authors")]
public sealed class ProductCoAuthorsController(CoAuthorService coAuthorService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CoAuthorResponse>>> GetAll(
        long productId, CancellationToken cancellationToken)
    {
        var result = await coAuthorService.GetByProductIdAsync(productId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CoAuthorResponse>> Add(
        long productId,
        [FromBody] AddCoAuthorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await coAuthorService.AddAsync(productId, request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { productId }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CoAuthorResponse>> Update(
        long productId,
        long id,
        [FromBody] UpdateCoAuthorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await coAuthorService.UpdateAsync(productId, id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Remove(
        long productId,
        long id,
        CancellationToken cancellationToken)
    {
        await coAuthorService.RemoveAsync(productId, id, cancellationToken);
        return NoContent();
    }
}
