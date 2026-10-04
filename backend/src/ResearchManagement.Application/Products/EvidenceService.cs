using ResearchManagement.Application.Common;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace ResearchManagement.Application.Products;

public sealed class EvidenceService(
    IProductRepository products,
    IProductEvidenceRepository evidences,
    ICurrentUser currentUser,
    IWebHostEnvironment environment)
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    private static readonly string[] AllowedExtensions =
        [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".jpeg", ".png", ".zip"];

    public async Task<IReadOnlyList<EvidenceResponse>> GetByProductIdAsync(
        long productId,
        CancellationToken cancellationToken)
    {
        _ = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        var list = await evidences.GetByProductIdAsync(productId, cancellationToken);
        return list.Select(ToResponse).ToList();
    }

    public async Task<EvidenceResponse> UploadAsync(
        long productId,
        IFormFile file,
        string? description,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        if (product.Status == ProductStatuses.Approved)
            throw new BusinessRuleException("Sản phẩm đã được duyệt hoàn toàn, không thể thêm minh chứng.");

        if (file.Length == 0)
            throw new BusinessRuleException("Tệp không được rỗng.");

        if (file.Length > MaxFileSizeBytes)
            throw new BusinessRuleException($"Tệp vượt quá kích thước tối đa {MaxFileSizeBytes / 1024 / 1024} MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new BusinessRuleException($"Định dạng tệp '{ext}' không được phép.");

        var actorId = currentUser.Id
            ?? throw new AuthenticationFailedException("Không xác định được người dùng hiện tại.");

        // Lưu file vào uploads/products/{productId}/
        var folder = Path.Combine(environment.WebRootPath, "uploads", "products", productId.ToString());
        Directory.CreateDirectory(folder);

        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, uniqueName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(stream, cancellationToken);

        var relativePath = Path.Combine("uploads", "products", productId.ToString(), uniqueName)
            .Replace('\\', '/');

        var evidence = new ProductEvidence(
            productId,
            actorId,
            file.FileName,
            relativePath,
            file.Length,
            description);

        await evidences.AddAsync(evidence, cancellationToken);
        await evidences.SaveChangesAsync(cancellationToken);

        return ToResponse(evidence);
    }

    public async Task DeleteAsync(
        long productId,
        long evidenceId,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        if (product.Status == ProductStatuses.Approved)
            throw new BusinessRuleException("Sản phẩm đã được duyệt hoàn toàn, không thể xóa minh chứng.");

        var evidence = await evidences.GetByIdAsync(evidenceId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy minh chứng.");

        if (evidence.ProductId != productId)
            throw new NotFoundException("Minh chứng không thuộc sản phẩm này.");

        // Xóa file vật lý
        var fullPath = Path.Combine(environment.WebRootPath, evidence.StoredPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        evidences.Remove(evidence);
        await evidences.SaveChangesAsync(cancellationToken);
    }

    private static EvidenceResponse ToResponse(ProductEvidence e) =>
        new(e.Id, e.ProductId, e.OriginalFileName, e.FileSizeBytes, e.Description, e.UploadedAt);
}
