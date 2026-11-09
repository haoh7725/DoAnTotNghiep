using ResearchManagement.Application.Common;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Application.ResearchPlans.Abstractions;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Application.ProductTypes.Abstractions;
using ResearchManagement.Application.Notifications.Abstractions;
using ResearchManagement.Application.ResearchTargets.Abstractions;
using ResearchManagement.Application.ResearchTargetAllocations.Abstractions;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Products;

public sealed class ProductService(
    IProductRepository products,
    IProductCoAuthorRepository coAuthors,
    IProductEvidenceRepository evidences,
    IReviewHistoryRepository reviewHistories,
    IResearchPlanItemRepository planItems,
    IResearchPlanRepository plans,
    IResearchTargetRepository targets,
    IResearchTargetAllocationRepository targetAllocations,
    ILecturerTargetAllocationRepository lecturerTargetAllocations,
    ILecturerAccessRepository lecturerAccess,
    IProductTypeRepository productTypes,
    INotificationRepository notifications,
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

        await EnsureCanReadAsync(product, cancellationToken);

        var coAuthorList = await coAuthors.GetByProductIdAsync(id, cancellationToken);
        var evidenceList = await evidences.GetByProductIdAsync(id, cancellationToken);

        return await ToResponseAsync(product, coAuthorList, evidenceList, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductSummaryResponse>> GetByPlanItemIdAsync(
        long planItemId,
        CancellationToken cancellationToken)
    {
        var planItem = await planItems.GetByIdAsync(planItemId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy nội dung kế hoạch.");

        var plan = await plans.GetByIdAsync(planItem.ResearchPlanId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy kế hoạch NCKH.");
        var access = await lecturerAccess.GetAccessInfoAsync(plan.LecturerId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");
        if (access.AccountId != GetCurrentAccountId() &&
            !currentUser.CanAccess(access.FacultyId, access.DepartmentId))
            throw new ForbiddenException("Không có quyền xem sản phẩm của nội dung kế hoạch này.");

        var list = await products.GetByPlanItemIdAsync(planItemId, cancellationToken);
        return list.Select(ToSummary).ToList();
    }

    public async Task<IReadOnlyList<ProductSummaryResponse>> GetMineAsync(CancellationToken cancellationToken)
    {
        var lecturer = await lecturers.GetByAccountIdAsync(GetCurrentAccountId(), cancellationToken)
            ?? throw new NotFoundException("Tài khoản chưa liên kết với giảng viên.");
        var list = await products.GetByLecturerIdAsync(lecturer.Id, cancellationToken);
        return list.Select(ToSummary).ToList();
    }

    public async Task<IReadOnlyList<ReviewHistoryResponse>> GetReviewHistoryAsync(
        long productId,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        await EnsureCanReadAsync(product, cancellationToken);

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

        var plan = await plans.GetByIdAsync(planItem.ResearchPlanId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy kế hoạch NCKH.");
        if (plan.LecturerId != lecturer.Id)
            throw new ForbiddenException("Chỉ được khai báo sản phẩm cho kế hoạch của chính mình.");
        if (plan.Status != "DA_DANG_KY")
            throw new BusinessRuleException("Phải đăng ký kế hoạch trước khi khai báo sản phẩm.");

        var productType = await productTypes.GetByIdAsync(planItem.ProductTypeId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy loại sản phẩm khoa học.");
        if (productType.Code == "BAI_BAO")
        {
            if (!request.ResearchTargetId.HasValue)
                throw new BusinessRuleException("Bài báo phải được gắn với một chỉ tiêu NCKH cụ thể.");
            var target = await targets.GetByIdAsync(request.ResearchTargetId.Value, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy chỉ tiêu NCKH.");
            var access = await lecturerAccess.GetAccessInfoAsync(lecturer.Id, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy đơn vị của giảng viên.");
            if (target.AcademicYearId != plan.AcademicYearId || target.FacultyId != access.FacultyId)
                throw new BusinessRuleException("Chỉ tiêu không thuộc năm học hoặc khoa của kế hoạch.");
            var allocation = await targetAllocations.GetByTargetAndDepartmentAsync(target.Id, access.DepartmentId, cancellationToken);
            if (allocation is null)
                throw new BusinessRuleException("Bộ môn của giảng viên chưa được phân bổ chỉ tiêu này.");
            var personalTarget = await lecturerTargetAllocations.GetAllocatedArticleCountAsync(allocation.Id, lecturer.Id, cancellationToken);
            if (!personalTarget.HasValue)
                throw new BusinessRuleException("Giảng viên chưa được phân công thực hiện chỉ tiêu này.");
            var registeredCount = await products.CountByTargetAndLecturerAsync(target.Id, lecturer.Id, cancellationToken);
            if (registeredCount >= personalTarget.Value)
                throw new BusinessRuleException("Số bài đăng ký đã đạt chỉ tiêu được phân công cho giảng viên.");
            if (target.JournalConferenceId != request.JournalConferenceId)
                throw new BusinessRuleException("Tạp chí/hội nghị của sản phẩm không khớp chỉ tiêu đã chọn.");
        }

        var product = new Product(
            planItem.Id,
            lecturer.Id,
            request.Title,
            request.Description,
            request.PublicationInfo,
            request.PublishedDate,
            request.ArticleStatus,
            request.JournalIndex,
            request.JournalClassification,
            request.ProjectLevel,
            request.JournalConferenceId,
            request.ResearchTargetId);

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
        await EnsureOwnerOrAdminAsync(product, cancellationToken);
        product.Update(request.Title, request.Description, request.PublicationInfo, request.PublishedDate,
            request.ArticleStatus, request.JournalIndex, request.JournalClassification, request.ProjectLevel);

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

        await EnsureOwnerOrAdminAsync(product, cancellationToken);

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

        var access = await lecturerAccess.GetAccessInfoAsync(product.SubmittedByLecturerId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên nộp sản phẩm.");
        if (!currentUser.IsInRole(Roles.Admin) &&
            !currentUser.CanAccess(access.FacultyId, access.DepartmentId))
            throw new ForbiddenException("Bạn không có quyền xét duyệt sản phẩm ngoài đơn vị được phân công.");

        if (request.ScoreEquivalent.HasValue &&
            product.Status != ProductStatuses.ResearchOfficeApproved &&
            !currentUser.IsInRole(Roles.Admin))
            throw new BusinessRuleException("Chỉ Phòng QLKH được nhập điểm quy đổi ở bước duyệt cuối.");

        var actorId = GetCurrentAccountId();
        var fromStatus = product.Status;

        if (action == "APPROVE")
        {
            var nextStatus = ApproveTransitions[product.Status];
            if (nextStatus == ProductStatuses.Approved)
                await EnsureReadyForFinalApprovalAsync(product, cancellationToken);
            product.Approve(nextStatus, request.ScoreEquivalent);
            var history = new ReviewHistory(product.Id, actorId, fromStatus, product.Status, request.Comment);
            await reviewHistories.AddAsync(history, cancellationToken);
        }
        else // REJECT
        {
            product.Return();
            var history = new ReviewHistory(product.Id, actorId, fromStatus, ProductStatuses.Returned, request.Comment);
            await reviewHistories.AddAsync(history, cancellationToken);

            var owner = await lecturers.GetByIdAsync(product.SubmittedByLecturerId, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy giảng viên nộp sản phẩm.");
            var reason = string.IsNullOrWhiteSpace(request.Comment)
                ? "Sản phẩm đã được trả lại để bổ sung."
                : $"Sản phẩm đã được trả lại: {request.Comment.Trim()}";
            var notification = new Notification(
                owner.AccountId,
                "PHAN_HOI",
                reason,
                $"TRA_LAI:PRODUCT:{product.Id}:{Guid.NewGuid():N}",
                scientificProductId: product.Id);
            notification.MarkAsSent();
            await notifications.AddAsync(notification, cancellationToken);
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

    private async Task EnsureOwnerOrAdminAsync(Product product, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(Roles.Admin)) return;
        var owner = await lecturers.GetByIdAsync(product.SubmittedByLecturerId, cancellationToken);
        if (owner?.AccountId != GetCurrentAccountId())
            throw new ForbiddenException("Chỉ tác giả chính được sửa hoặc gửi duyệt sản phẩm.");
    }

    private async Task EnsureCanReadAsync(Product product, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(Roles.Admin)) return;

        var accountId = GetCurrentAccountId();
        var access = await lecturerAccess.GetAccessInfoAsync(product.SubmittedByLecturerId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên nộp sản phẩm.");
        if (access.AccountId == accountId ||
            currentUser.CanAccess(access.FacultyId, access.DepartmentId))
            return;

        var authors = await coAuthors.GetByProductIdAsync(product.Id, cancellationToken);
        foreach (var author in authors)
        {
            var lecturer = await lecturers.GetByIdAsync(author.LecturerId, cancellationToken);
            if (lecturer?.AccountId == accountId) return;
        }

        throw new ForbiddenException("Không có quyền xem sản phẩm này.");
    }

    private async Task EnsureReadyForFinalApprovalAsync(Product product, CancellationToken cancellationToken)
    {
        var item = await planItems.GetByIdAsync(product.ResearchPlanItemId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy nội dung kế hoạch của sản phẩm.");
        var type = await productTypes.GetByIdAsync(item.ProductTypeId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy loại sản phẩm khoa học.");
        if (type.Code == "BAI_BAO" &&
            (product.ArticleStatus != "DA_XUAT_BAN" || !product.PublishedDate.HasValue))
            throw new BusinessRuleException("Bài báo chỉ được duyệt hoàn tất khi đã xuất bản và có ngày công bố.");
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
                ca.DisplayOrder,
                ca.AuthorRole));
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
            product.ArticleStatus,
            product.JournalIndex,
            product.JournalClassification,
            product.ProjectLevel,
            product.JournalConferenceId,
            product.ResearchTargetId,
            product.Status,
            product.ScoreEquivalent,
            product.CreatedAt,
            product.UpdatedAt,
            coAuthorResponses,
            evidenceResponses);
    }

    private static ProductSummaryResponse ToSummary(Product p) =>
        new(p.Id, p.ResearchPlanItemId, p.Title, p.ArticleStatus, p.Status, p.ScoreEquivalent, p.CreatedAt, p.UpdatedAt);
}
