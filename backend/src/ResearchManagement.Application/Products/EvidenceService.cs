using ResearchManagement.Application.Common;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace ResearchManagement.Application.Products;

public sealed class EvidenceService(
    IProductRepository products,
    IProductEvidenceRepository evidences,
    ILecturerRepository lecturers,
    ICurrentUser currentUser,
    IFileStorage fileStorage)
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

        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var objectKey = $"uploads/products/{productId}/{uniqueName}";

        await using var stream = file.OpenReadStream();
        await fileStorage.PutAsync(
            objectKey,
            stream,
            file.Length,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            cancellationToken);

        var evidence = new ProductEvidence(
            productId,
            actorId,
            file.FileName,
            objectKey,
            file.Length,
            description);

        try
        {
            await evidences.AddAsync(evidence, cancellationToken);
            await evidences.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await fileStorage.DeleteAsync(objectKey, CancellationToken.None);
            throw;
        }

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

        await fileStorage.DeleteAsync(evidence.StoredPath, cancellationToken);

        evidences.Remove(evidence);
        await evidences.SaveChangesAsync(cancellationToken);
    }

    private static EvidenceResponse ToResponse(ProductEvidence e) =>
        new(e.Id, e.ProductId, e.OriginalFileName, e.FileSizeBytes, e.Description, e.UploadedAt);

    public async Task<(Stream Content, string FileName)> GetDownloadAsync(
        long productId, long evidenceId, CancellationToken cancellationToken)
    {
        _ = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        var evidence = await evidences.GetByIdAsync(evidenceId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy minh chứng.");
        if (evidence.ProductId != productId)
            throw new NotFoundException("Minh chứng không thuộc sản phẩm này.");
        var content = await fileStorage.OpenReadAsync(evidence.StoredPath, cancellationToken);
        return (content, evidence.OriginalFileName);
    }

    private async Task EnsureOwnerOrAdminAsync(Product product, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(Roles.Admin)) return;
        var owner = await lecturers.GetByIdAsync(product.SubmittedByLecturerId, cancellationToken);
        if (owner?.AccountId != currentUser.Id)
            throw new ForbiddenException("Chỉ tác giả chính được quản lý minh chứng.");
    }
}
