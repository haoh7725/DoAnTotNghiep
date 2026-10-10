using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using ResearchManagement.Application.Products;
using ResearchManagement.Application.Products.Models;

namespace ResearchManagement.Api.Controllers;

[ApiController, Authorize, Route("api/products/{productId:long}/evidences")]
public sealed class ProductEvidencesController(EvidenceService evidenceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EvidenceResponse>>> List(long productId, CancellationToken ct) =>
        Ok(await evidenceService.GetByProductIdAsync(productId, ct));

    [HttpPost, RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<EvidenceResponse>> Upload(
        long productId, IFormFile file, [FromForm] string? description, CancellationToken ct)
    {
        var result = await evidenceService.UploadAsync(productId, file, description, ct);
        return CreatedAtAction(nameof(Download), new { productId, id = result.Id }, result);
    }

    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> Download(long productId, long id, CancellationToken ct)
    {
        var stored = await evidenceService.GetDownloadAsync(productId, id, ct);
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(stored.FileName, out var contentType)) contentType = "application/octet-stream";
        return PhysicalFile(stored.Path, contentType, stored.FileName, enableRangeProcessing: true);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long productId, long id, CancellationToken ct)
    {
        await evidenceService.DeleteAsync(productId, id, ct);
        return NoContent();
    }
}
