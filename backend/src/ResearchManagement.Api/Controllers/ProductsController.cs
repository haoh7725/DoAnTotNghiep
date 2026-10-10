using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Products;
using ResearchManagement.Application.Products.Models;

namespace ResearchManagement.Api.Controllers;

/// <summary>
/// Sản phẩm khoa học (bài báo, đề tài, sách, chứng nhận), trạng thái bài báo và tác giả.
/// Controller chỉ yêu cầu đăng nhập; quyền xem/sửa theo người tạo, tác giả và phạm vi do service kiểm tra.
/// </summary>
[ApiController]
[Route("api/products")]
[Authorize]
public sealed class ProductsController(
    ProductService products,
    ProductAuthorService authors) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductSummaryResponse>>> Search(
        [FromQuery] ProductSearchQuery query, CancellationToken cancellationToken) =>
        Ok(await products.SearchAsync(query, cancellationToken));

    /// <summary>Tìm giảng viên toàn trường để chọn làm đồng tác giả (tối đa 10 kết quả).</summary>
    [HttpGet("author-candidates")]
    public async Task<ActionResult<IReadOnlyList<AuthorCandidateResponse>>> SearchAuthorCandidates(
        [FromQuery] string? keyword, CancellationToken cancellationToken) =>
        Ok(await authors.SearchCandidatesAsync(keyword, cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductDetailResponse>> GetById(long id, CancellationToken cancellationToken) =>
        Ok(await products.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ProductDetailResponse>> Create(
        [FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await products.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ProductDetailResponse>> Update(
        long id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken) =>
        Ok(await products.UpdateAsync(id, request, cancellationToken));

    /// <summary>Đổi tiến độ bài báo: Đang viết → Đang phản biện → Đã nhận xét → Đã xuất bản.</summary>
    [HttpPut("{id:long}/article-status")]
    public async Task<ActionResult<ProductDetailResponse>> ChangeArticleStatus(
        long id, [FromBody] ChangeArticleStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await products.ChangeArticleStatusAsync(id, request, cancellationToken));

    [HttpGet("{id:long}/authors")]
    public async Task<ActionResult<IReadOnlyList<ProductAuthorResponse>>> GetAuthors(
        long id, CancellationToken cancellationToken) =>
        Ok(await authors.GetAsync(id, cancellationToken));

    /// <summary>Thay toàn bộ danh sách tác giả; thứ tự trong body là thứ tự tác giả.</summary>
    [HttpPut("{id:long}/authors")]
    public async Task<ActionResult<IReadOnlyList<ProductAuthorResponse>>> ReplaceAuthors(
        long id, [FromBody] ReplaceProductAuthorsRequest request, CancellationToken cancellationToken) =>
        Ok(await authors.ReplaceAsync(id, request, cancellationToken));

    [HttpPost("{id:long}/authors")]
    public async Task<ActionResult<IReadOnlyList<ProductAuthorResponse>>> AddAuthor(
        long id, [FromBody] AddProductAuthorRequest request, CancellationToken cancellationToken)
    {
        var result = await authors.AddAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetAuthors), new { id }, result);
    }

    [HttpDelete("{id:long}/authors/{lecturerId:long}")]
    public async Task<ActionResult<IReadOnlyList<ProductAuthorResponse>>> RemoveAuthor(
        long id, long lecturerId, CancellationToken cancellationToken) =>
        Ok(await authors.RemoveAsync(id, lecturerId, cancellationToken));
}
