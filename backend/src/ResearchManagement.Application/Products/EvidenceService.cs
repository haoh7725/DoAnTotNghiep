using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Products;

public sealed record EvidenceDownloadResult(Stream Stream, string ContentType, string FileName);

public sealed class EvidenceService(
    IProductRepository products,
    IProductEvidenceRepository evidences,
    IFileStorageService fileStorage,
    ICurrentUser currentUser)
{
    public const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".zip"] = "application/zip",
        [".rar"] = "application/x-rar-compressed",
        [".7z"] = "application/x-7z-compressed"
    };

    public async Task<IReadOnlyList<EvidenceResponse>> GetByProductIdAsync(
        long productId,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        var authors = await products.GetAuthorsAsync(productId, cancellationToken);

        if (!ProductAccess.CanRead(currentUser, product.CreatedByAccountId, product.ReviewStatus, authors))
            throw new ForbiddenException("Không có quyền xem minh chứng của sản phẩm này.");

        return await evidences.GetViewsByProductIdAsync(productId, cancellationToken);
    }

    public async Task<EvidenceResponse> UploadAsync(
        long productId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        var authors = await products.GetAuthorsAsync(productId, cancellationToken);

        if (!ProductAccess.CanRead(currentUser, product.CreatedByAccountId, product.ReviewStatus, authors))
            throw new ForbiddenException("Không có quyền xem sản phẩm này.");

        if (!ProductAccess.CanManage(currentUser, product.CreatedByAccountId, authors))
            throw new ForbiddenException("Chỉ người tạo hoặc tác giả chính mới có quyền tải lên minh chứng.");

        if (!ReviewStatuses.IsEditable(product.ReviewStatus))
            throw new BusinessRuleException(
                "Sản phẩm đã gửi duyệt hoặc đã chốt nên không thể thay đổi minh chứng. Chỉ có thể thêm minh chứng khi sản phẩm ở trạng thái Nháp hoặc Cần bổ sung.");

        if (file.Length == 0)
            throw new BusinessRuleException("Tệp tải lên không được để trống.");

        if (file.Length > MaxFileSizeBytes)
            throw new BusinessRuleException(
                $"Dung lượng tệp ({file.Length / (1024.0 * 1024.0):0.#} MB) vượt quá giới hạn tối đa cho phép ({MaxFileSizeBytes / 1024 / 1024} MB).");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedTypes.TryGetValue(ext, out var detectedMimeType))
            throw new BusinessRuleException(
                $"Định dạng tệp '{ext}' không được phép. Hệ thống chỉ chấp nhận: PDF, Word, Excel, PowerPoint, Ảnh (JPG, PNG) và tệp nén (ZIP, RAR, 7Z).");

        var actorId = currentUser.Id
            ?? throw new AuthenticationFailedException("Không xác định được người dùng hiện tại.");

        var originalName = Path.GetFileName(file.FileName).Trim();
        if (string.IsNullOrEmpty(originalName))
            originalName = $"minh_chung_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        if (originalName.Length > 255)
            originalName = originalName[..255];

        // Tính mã băm SHA256 để bảo đảm tính toàn vẹn tệp
        string sha256Hex;
        using (var sha = SHA256.Create())
        await using (var readStream = file.OpenReadStream())
        {
            var hashBytes = await sha.ComputeHashAsync(readStream, cancellationToken);
            sha256Hex = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        // Lưu tệp an toàn vào thư mục lưu trữ với tên tệp duy nhất
        var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
        string storageKey;
        await using (var uploadStream = file.OpenReadStream())
        {
            storageKey = await fileStorage.SaveAsync(
                $"products/{productId}",
                uniqueFileName,
                uploadStream,
                cancellationToken);
        }

        var evidence = new ProductEvidence(
            productId,
            actorId,
            originalName,
            storageKey,
            detectedMimeType,
            file.Length,
            sha256Hex);

        await evidences.AddAsync(evidence, cancellationToken);
        await evidences.SaveChangesAsync(cancellationToken);

        var view = await evidences.GetViewByIdAsync(evidence.Id, cancellationToken);
        return view ?? new EvidenceResponse(
            evidence.Id,
            evidence.ProductId,
            evidence.UploadedByAccountId,
            "Tôi",
            evidence.FileName,
            evidence.MimeType,
            evidence.FileSizeBytes,
            evidence.Sha256,
            evidence.UploadedAt);
    }

    public async Task<EvidenceDownloadResult> GetFileForDownloadAsync(
        long productId,
        long evidenceId,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        var authors = await products.GetAuthorsAsync(productId, cancellationToken);

        if (!ProductAccess.CanRead(currentUser, product.CreatedByAccountId, product.ReviewStatus, authors))
            throw new ForbiddenException("Không có quyền xem hoặc tải minh chứng của sản phẩm này.");

        var evidence = await evidences.GetByIdAsync(evidenceId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy minh chứng.");

        if (evidence.ProductId != productId)
            throw new NotFoundException("Minh chứng không thuộc sản phẩm này.");

        var stream = await fileStorage.OpenReadAsync(evidence.StorageKey, cancellationToken)
            ?? throw new NotFoundException("Tệp minh chứng không tồn tại trên hệ thống lưu trữ.");

        return new EvidenceDownloadResult(stream, evidence.MimeType, evidence.FileName);
    }

    public async Task DeleteAsync(
        long productId,
        long evidenceId,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        var authors = await products.GetAuthorsAsync(productId, cancellationToken);

        if (!ProductAccess.CanRead(currentUser, product.CreatedByAccountId, product.ReviewStatus, authors))
            throw new ForbiddenException("Không có quyền truy cập sản phẩm này.");

        if (!ProductAccess.CanManage(currentUser, product.CreatedByAccountId, authors))
            throw new ForbiddenException("Chỉ người tạo hoặc tác giả chính mới có quyền xóa minh chứng.");

        if (!ReviewStatuses.IsEditable(product.ReviewStatus))
            throw new BusinessRuleException(
                "Sản phẩm đã gửi duyệt hoặc đã chốt nên không thể thay đổi minh chứng. Chỉ có thể xóa minh chứng khi sản phẩm ở trạng thái Nháp hoặc Cần bổ sung.");

        var evidence = await evidences.GetByIdAsync(evidenceId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy minh chứng.");

        if (evidence.ProductId != productId)
            throw new NotFoundException("Minh chứng không thuộc sản phẩm này.");

        evidences.Remove(evidence);
        await evidences.SaveChangesAsync(cancellationToken);

        await fileStorage.DeleteAsync(evidence.StorageKey, cancellationToken);
    }
}
