using ResearchManagement.Application.Common;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Application.ResearchPlans.Abstractions;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Products;

public sealed class ProductService(
    IProductRepository products,
    IProductCoAuthorRepository coAuthors,
    IProductEvidenceRepository evidences,
    IReviewHistoryRepository reviewHistories,
    IResearchPlanItemRepository planItems,
    ILecturerRepository lecturers,
    ICurrentUser currentUser)
{
    // ──── Chuyển trạng thái hợp lệ khi APPROVE ───────────────────────────────
    private static readonly Dictionary<string, string> ApproveTransitions = new()
    {
        [ProductStatuses.Submitted]               = ProductStatuses.DepartmentHeadApproved,
        [ProductStatuses.DepartmentHeadApproved]  = ProductStatuses.FacultyDeanApproved,
        [ProductStatuses.FacultyDeanApproved]     = ProductStatuses.ResearchOfficeApproved,
        [ProductStatuses.ResearchOfficeApproved]  = ProductStatuses.Approved,
    };

    // Vai trò nào được duyệt từ trạng thái nào
    private static readonly Dictionary<string, string> ApproverRoleForStatus = new()
    {
        [ProductStatuses.Submitted]               = Roles.DepartmentHead,
        [ProductStatuses.DepartmentHeadApproved]  = Roles.FacultyDean,
        [ProductStatuses.FacultyDeanApproved]     = Roles.ResearchOffice,
        [ProductStatuses.ResearchOfficeApproved]  = Roles.ResearchOffice,
    };

    // ──── Queries ─────────────────────────────────────────────────────────────

    public async Task<ProductResponse> GetByIdAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        var coAuthorList = await coAuthors.GetByProductIdAsync(id, cancellationToken);
        var evidenceList = await evidences.GetByProductIdAsync(id, cancellationToken);

        return await ToResponseAsync(product, coAuthorList, evidenceList, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductSummaryResponse>> GetByPlanItemIdAsync(
        long planItemId,
        CancellationToken cancellationToken)
    {
        _ = await planItems.GetByIdAsync(planItemId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy nội dung kế hoạch.");

        var list = await products.GetByPlanItemIdAsync(planItemId, cancellationToken);
        return list.Select(ToSummary).ToList();
    }

    public async Task<IReadOnlyList<ReviewHistoryResponse>> GetReviewHistoryAsync(
        long productId,
        CancellationToken cancellationToken)
    {
        _ = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        var histories = await reviewHistories.GetByProductIdAsync(productId, cancellationToken);
        return histories.Select(h => new ReviewHistoryResponse(
            h.Id,
            h.ProductId,
            h.ActorAccountId,
            string.Empty, // username cần join — trả về rỗng, controller hoặc repository sẽ enrich nếu cần
            h.FromStatus,
            h.ToStatus,
            h.Comment,
            h.OccurredAt)).ToList();
    }

    // ──── Commands ────────────────────────────────────────────────────────────

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetCurrentAccountId();

        var planItem = await planItems.GetByIdAsync(request.ResearchPlanItemId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy nội dung kế hoạch.");

        // Lấy giảng viên của người dùng hiện tại
        var lecturer = await lecturers.GetByAccountIdAsync(actorId, cancellationToken)
            ?? throw new BusinessRuleException("Tài khoản này chưa được liên kết với giảng viên.");

        var product = new Product(
            planItem.Id,
            lecturer.Id,
            request.Title,
            request.Description,
            request.PublicationInfo,
            request.PublishedDate);

        await products.AddAsync(product, cancellationToken);
        await products.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(product, [], [], cancellationToken);
    }

    public async Task<ProductResponse> UpdateAsync(
        long id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        EnsureEditable(product);
        EnsureOwnerOrAdmin(product);

        product.Update(request.Title, request.Description, request.PublicationInfo, request.PublishedDate);

        await products.SaveChangesAsync(cancellationToken);

        var coAuthorList = await coAuthors.GetByProductIdAsync(id, cancellationToken);
        var evidenceList = await evidences.GetByProductIdAsync(id, cancellationToken);
        return await ToResponseAsync(product, coAuthorList, evidenceList, cancellationToken);
    }

    public async Task<ProductResponse> SubmitAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        if (product.Status is not (ProductStatuses.Draft or ProductStatuses.Returned))
            throw new BusinessRuleException("Chỉ có thể nộp sản phẩm ở trạng thái Nháp hoặc Trả lại.");

        EnsureOwnerOrAdmin(product);

        var actorId = GetCurrentAccountId();
        var fromStatus = product.Status;

        product.Submit();

        var history = new ReviewHistory(product.Id, actorId, fromStatus, product.Status, null);
        await reviewHistories.AddAsync(history, cancellationToken);

        await products.SaveChangesAsync(cancellationToken);

        var coAuthorList = await coAuthors.GetByProductIdAsync(id, cancellationToken);
        var evidenceList = await evidences.GetByProductIdAsync(id, cancellationToken);
        return await ToResponseAsync(product, coAuthorList, evidenceList, cancellationToken);
    }

    public async Task<ProductResponse> ReviewAsync(
        long id,
        ReviewProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        var action = request.Action.Trim().ToUpperInvariant();
        if (action is not ("APPROVE" or "REJECT"))
            throw new BusinessRuleException("Hành động xét duyệt phải là APPROVE hoặc REJECT.");

        // Kiểm tra trạng thái có thể duyệt không
        if (!ApproverRoleForStatus.TryGetValue(product.Status, out var requiredRole))
            throw new BusinessRuleException($"Sản phẩm ở trạng thái '{product.Status}' không thể xét duyệt.");

        // Kiểm tra vai trò người duyệt
        if (!currentUser.IsInRole(requiredRole) && !currentUser.IsInRole(Roles.Admin))
            throw new ForbiddenException("Bạn không có quyền xét duyệt ở bước này.");

        var actorId = GetCurrentAccountId();
        var fromStatus = product.Status;

        if (action == "APPROVE")
        {
            var nextStatus = ApproveTransitions[product.Status];
            product.Approve(nextStatus, request.ScoreEquivalent);
            var history = new ReviewHistory(product.Id, actorId, fromStatus, product.Status, request.Comment);
            await reviewHistories.AddAsync(history, cancellationToken);
        }
        else // REJECT
        {
            product.Return();
            var history = new ReviewHistory(product.Id, actorId, fromStatus, ProductStatuses.Returned, request.Comment);
            await reviewHistories.AddAsync(history, cancellationToken);
        }

        await products.SaveChangesAsync(cancellationToken);

        var coAuthorList = await coAuthors.GetByProductIdAsync(id, cancellationToken);
        var evidenceList = await evidences.GetByProductIdAsync(id, cancellationToken);
        return await ToResponseAsync(product, coAuthorList, evidenceList, cancellationToken);
    }

    // ──── Private helpers ─────────────────────────────────────────────────────

    private long GetCurrentAccountId() =>
        currentUser.Id ?? throw new AuthenticationFailedException("Không xác định được người dùng hiện tại.");

    private static void EnsureEditable(Product product)
    {
        if (product.Status is not (ProductStatuses.Draft or ProductStatuses.Returned))
            throw new BusinessRuleException("Chỉ có thể chỉnh sửa sản phẩm ở trạng thái Nháp hoặc Trả lại.");
    }

    private void EnsureOwnerOrAdmin(Product product)
    {
        if (currentUser.IsInRole(Roles.Admin)) return;

        // Người dùng phải là chủ sản phẩm — kiểm tra qua lecturer
        // (kiểm tra đơn giản: không throw nếu Admin, các trường hợp khác để controller quyết định)
        // Thực tế cần join với lecturer để so sánh SubmittedByLecturerId với currentUser
        // — bổ sung khi cần phân quyền sở hữu chặt hơn.
    }

    private async Task<ProductResponse> ToResponseAsync(
        Product product,
        List<ProductCoAuthor> coAuthorList,
        List<ProductEvidence> evidenceList,
        CancellationToken cancellationToken)
    {
        // Lấy tên tác giả chính
        var ownerLecturer = await lecturers.GetByIdAsync(product.SubmittedByLecturerId, cancellationToken);
        var ownerName = ownerLecturer?.FullName ?? string.Empty;

        // Enrich đồng tác giả
        var coAuthorResponses = new List<CoAuthorResponse>();
        foreach (var ca in coAuthorList.OrderBy(c => c.DisplayOrder))
        {
            var lec = await lecturers.GetByIdAsync(ca.LecturerId, cancellationToken);
            coAuthorResponses.Add(new CoAuthorResponse(
                ca.Id, ca.ProductId, ca.LecturerId,
                lec?.FullName ?? string.Empty,
                lec?.Code,
                ca.DisplayOrder));
        }

        var evidenceResponses = evidenceList.Select(e => new EvidenceResponse(
            e.Id, e.ProductId, e.OriginalFileName, e.FileSizeBytes, e.Description, e.UploadedAt)).ToList();

        return new ProductResponse(
            product.Id,
            product.ResearchPlanItemId,
            product.SubmittedByLecturerId,
            ownerName,
            product.Title,
            product.Description,
            product.PublicationInfo,
            product.PublishedDate,
            product.Status,
            product.ScoreEquivalent,
            product.CreatedAt,
            product.UpdatedAt,
            coAuthorResponses,
            evidenceResponses);
    }

    private static ProductSummaryResponse ToSummary(Product p) =>
        new(p.Id, p.ResearchPlanItemId, p.Title, p.Status, p.ScoreEquivalent, p.CreatedAt, p.UpdatedAt);
}
