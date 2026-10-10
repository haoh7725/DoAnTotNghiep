using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.Products;
using ResearchManagement.Application.Products.Models;

namespace ResearchManagement.Api.Controllers;

/// <summary>
/// Quản lý minh chứng đính kèm sản phẩm nghiên cứu khoa học:
/// upload, xem trực tiếp (inline view), tải về (download) và xóa minh chứng.
/// </summary>
[ApiController]
[Route("api/products/{productId:long}/evidences")]
[Authorize]
public sealed class ProductEvidencesController(EvidenceService evidences) : ControllerBase
{
    /// <summary>Xem danh sách minh chứng của một sản phẩm.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EvidenceResponse>>> GetByProductId(
        long productId, CancellationToken cancellationToken) =>
        Ok(await evidences.GetByProductIdAsync(productId, cancellationToken));

    /// <summary>Tải lên tệp minh chứng mới cho sản phẩm (chỉ chấp nhận PDF, Word, Excel, PPT, Ảnh, Zip, tối đa 25MB).</summary>
    [HttpPost]
    public async Task<ActionResult<EvidenceResponse>> Upload(
        long productId, IFormFile file, CancellationToken cancellationToken)
    {
        var result = await evidences.UploadAsync(productId, file, cancellationToken);
        return CreatedAtAction(nameof(GetByProductId), new { productId }, result);
    }

    /// <summary>Xem trực tiếp tệp minh chứng trên trình duyệt (inline preview).</summary>
    [HttpGet("{evidenceId:long}/view")]
    public async Task<IActionResult> ViewFile(
        long productId, long evidenceId, CancellationToken cancellationToken)
    {
        var result = await evidences.GetFileForDownloadAsync(productId, evidenceId, cancellationToken);
        Response.Headers.ContentDisposition = $"inline; filename=\"{Uri.EscapeDataString(result.FileName)}\"";
        return File(result.Stream, result.ContentType, enableRangeProcessing: true);
    }

    /// <summary>Tải về tệp minh chứng với tên tệp gốc.</summary>
    [HttpGet("{evidenceId:long}/download")]
    public async Task<IActionResult> DownloadFile(
        long productId, long evidenceId, CancellationToken cancellationToken)
    {
        var result = await evidences.GetFileForDownloadAsync(productId, evidenceId, cancellationToken);
        return File(result.Stream, result.ContentType, result.FileName, enableRangeProcessing: true);
    }

    /// <summary>Xóa tệp minh chứng (chỉ người tạo/tác giả chính khi sản phẩm ở trạng thái Nháp hoặc Cần bổ sung).</summary>
    [HttpDelete("{evidenceId:long}")]
    public async Task<IActionResult> Delete(
        long productId, long evidenceId, CancellationToken cancellationToken)
    {
        await evidences.DeleteAsync(productId, evidenceId, cancellationToken);
        return NoContent();
    }
}
