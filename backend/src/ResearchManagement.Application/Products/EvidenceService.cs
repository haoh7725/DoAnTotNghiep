using ResearchManagement.Application.Common;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ResearchManagement.Application.Products;

public sealed class EvidenceService(
    IProductRepository products,
    IProductEvidenceRepository evidences,
    ILecturerRepository lecturers,
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
        await EnsureOwnerOrAdminAsync(product, cancellationToken);

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
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "products", productId.ToString());
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
        await EnsureOwnerOrAdminAsync(product, cancellationToken);

        var evidence = await evidences.GetByIdAsync(evidenceId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy minh chứng.");

        if (evidence.ProductId != productId)
            throw new NotFoundException("Minh chứng không thuộc sản phẩm này.");

        // Xóa file vật lý
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, evidence.StoredPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        evidences.Remove(evidence);
        await evidences.SaveChangesAsync(cancellationToken);
    }

    private static EvidenceResponse ToResponse(ProductEvidence e) =>
        new(e.Id, e.ProductId, e.OriginalFileName, e.FileSizeBytes, e.Description, e.UploadedAt);

    public async Task<(string Path, string FileName)> GetDownloadAsync(
        long productId, long evidenceId, CancellationToken cancellationToken)
    {
        _ = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        var evidence = await evidences.GetByIdAsync(evidenceId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy minh chứng.");
        if (evidence.ProductId != productId)
            throw new NotFoundException("Minh chứng không thuộc sản phẩm này.");
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, evidence.StoredPath.Replace('/', Path.DirectorySeparatorChar)));
        var allowedRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", "products")) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            throw new NotFoundException("Không tìm thấy tệp minh chứng.");
        return (fullPath, evidence.OriginalFileName);
    }

    private async Task EnsureOwnerOrAdminAsync(Product product, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(Roles.Admin)) return;
        var owner = await lecturers.GetByIdAsync(product.SubmittedByLecturerId, cancellationToken);
        if (owner?.AccountId != currentUser.Id)
            throw new ForbiddenException("Chỉ tác giả chính được quản lý minh chứng.");
    }
}
