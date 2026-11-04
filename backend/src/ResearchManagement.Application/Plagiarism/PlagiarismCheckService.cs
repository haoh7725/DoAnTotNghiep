using System.Security.Cryptography;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Application.Plagiarism.Abstractions;
using ResearchManagement.Application.Plagiarism.Models;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Plagiarism;

public sealed class PlagiarismCheckService(
    IProductRepository products,
    IProductEvidenceRepository evidences,
    IPlagiarismCheckRepository checks,
    ILecturerRepository lecturers,
    ICurrentUser currentUser,
    IFileStorage fileStorage,
    IPlagiarismProvider provider)
{
    private static readonly string[] SupportedExtensions = [".pdf", ".doc", ".docx", ".txt"];

    public async Task<IReadOnlyList<PlagiarismCheckResponse>> ListAsync(long productId, CancellationToken ct)
    {
        _ = await products.GetByIdAsync(productId, ct) ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        return (await checks.GetByProductIdAsync(productId, ct)).Select(ToResponse).ToList();
    }

    public async Task<PlagiarismCheckResponse> CreateAsync(long productId,
        CreatePlagiarismCheckRequest request, CancellationToken ct)
    {
        if (!request.Consent)
            throw new BusinessRuleException("Bạn phải đồng ý gửi tài liệu đến Copyleaks.");
        var product = await products.GetByIdAsync(productId, ct) ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        await EnsureOwnerOrAdminAsync(product, ct);
        var evidence = await evidences.GetByIdAsync(request.EvidenceId, ct) ?? throw new NotFoundException("Không tìm thấy minh chứng.");
        if (evidence.ProductId != productId) throw new NotFoundException("Minh chứng không thuộc sản phẩm này.");
        var extension = Path.GetExtension(evidence.OriginalFileName).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
            throw new BusinessRuleException("Copyleaks chỉ nhận PDF, DOC, DOCX hoặc TXT trong chức năng này.");

        byte[] content;
        await using (var stream = await fileStorage.OpenReadAsync(evidence.StoredPath, ct))
        await using (var memory = new MemoryStream())
        {
            await stream.CopyToAsync(memory, ct);
            content = memory.ToArray();
        }
        var hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        if (await checks.HasActiveCheckAsync(evidence.Id, hash, ct))
            throw new BusinessRuleException("Tài liệu này đang được kiểm tra. Vui lòng chờ kết quả.");

        var actorId = currentUser.Id ?? throw new AuthenticationFailedException("Không xác định được người dùng.");
        var item = new PlagiarismCheck(productId, evidence.Id, actorId, hash, true);
        await checks.AddAsync(item, ct);
        await checks.SaveChangesAsync(ct);

        var scanIdCandidate = $"rh-{item.Id}-{Guid.NewGuid():N}";
        var scanId = scanIdCandidate[..Math.Min(36, scanIdCandidate.Length)];
        item.StartSending(scanId);
        await checks.SaveChangesAsync(ct);
        try
        {
            await provider.SubmitAsync(new PlagiarismSubmission(scanId, evidence.OriginalFileName, content, item.Id.ToString()), ct);
            item.MarkProcessing();
        }
        catch (Exception ex)
        {
            item.Fail(ex is BusinessRuleException ? ex.Message : "Không thể gửi tài liệu đến Copyleaks.");
        }
        await checks.SaveChangesAsync(CancellationToken.None);
        return ToResponse(item);
    }

    public async Task HandleCompletedAsync(CopyleaksWebhookResult result, CancellationToken ct)
    {
        var item = await checks.GetByExternalScanIdAsync(result.ScanId, ct)
            ?? throw new NotFoundException("Không tìm thấy tác vụ Copyleaks.");
        if (item.Status == PlagiarismCheckStatuses.Completed) return;
        item.Complete(result.SimilarityScore, result.SourceCount, result.TotalWords, result.CreditsUsed, result.ReportUrl);
        await checks.SaveChangesAsync(ct);
    }

    public async Task HandleErrorAsync(string scanId, string message, CancellationToken ct)
    {
        var item = await checks.GetByExternalScanIdAsync(scanId, ct)
            ?? throw new NotFoundException("Không tìm thấy tác vụ Copyleaks.");
        if (item.Status == PlagiarismCheckStatuses.Completed) return;
        item.Fail(string.IsNullOrWhiteSpace(message) ? "Copyleaks không thể hoàn thành kiểm tra." : message);
        await checks.SaveChangesAsync(ct);
    }

    private async Task EnsureOwnerOrAdminAsync(Product product, CancellationToken ct)
    {
        if (currentUser.IsInRole(Roles.Admin)) return;
        var owner = await lecturers.GetByIdAsync(product.SubmittedByLecturerId, ct);
        if (owner?.AccountId != currentUser.Id)
            throw new ForbiddenException("Chỉ tác giả chính được yêu cầu kiểm tra tương đồng.");
    }

    private static PlagiarismCheckResponse ToResponse(PlagiarismCheck x) => new(
        x.Id, x.ProductId, x.EvidenceId, x.Provider, x.Status, x.SimilarityScore,
        x.MatchedSourceCount, x.TotalWords, x.CreditsUsed, x.ReportUrl, x.ErrorMessage,
        x.RequestedByAccountId, x.CreatedAt, x.StartedAt, x.CompletedAt);
}
