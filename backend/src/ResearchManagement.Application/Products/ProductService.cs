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
    IAccountRepository accounts,
    TimeProvider clock,
    ICurrentUser currentUser)
{
    // ──── Chuyển trạng thái hợp lệ khi APPROVE ───────────────────────────────
    private static readonly Dictionary<string, string> ApproveTransitions = new()
    {
        [ProductStatuses.Submitted]               = ProductStatuses.DepartmentHeadApproved,
        [ProductStatuses.DepartmentHeadApproved]  = ProductStatuses.FacultyDeanApproved,
        // Phòng QLKH thẩm định và công nhận trong một bước.
        [ProductStatuses.FacultyDeanApproved]     = ProductStatuses.Approved,
        // Giữ tương thích với dữ liệu cũ đang dừng ở bước QLKH đã thẩm định.
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
            if (request.ResearchTargetId.HasValue)
                await ValidateTargetLinkAsync(lecturer.Id, plan, request.ResearchTargetId.Value,
                    request.JournalConferenceId, cancellationToken);
        }
        else if (request.ResearchTargetId.HasValue)
            throw new BusinessRuleException("Chỉ bài báo mới được liên kết chỉ tiêu bài báo.");

        var product = new Product(
            planItem.Id,
            lecturer.Id,
            request.Title,
            request.Description,
            request.PublicationInfo,
            request.PublishedDate,
            ToStoredArticleStatus(request.ArticleStatus),
            request.JournalIndex,
            request.JournalClassification,
            request.ProjectLevel,
            request.JournalConferenceId,
            request.ResearchTargetId);
        product.SetExternalAuthorCount(request.ExternalAuthorCount);
        product.ClassificationId = request.ClassificationId;

        if (planItem.Status == "CHUA_THUC_HIEN")
            planItem.UpdateProgress("DANG_THUC_HIEN", planItem.Result, planItem.DifficultyProposal);

        await products.AddAsync(product, cancellationToken);
        await products.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(product, [], [], cancellationToken);
    }

    public async Task<ProductResponse> LinkTargetAsync(long id, long targetId, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        var lecturer = await lecturers.GetByAccountIdAsync(GetCurrentAccountId(), cancellationToken)
            ?? throw new ForbiddenException("Tài khoản chưa liên kết với giảng viên.");
        if (product.SubmittedByLecturerId != lecturer.Id)
            throw new ForbiddenException("Chỉ tác giả chính được liên kết chỉ tiêu.");
        if (product.ResearchTargetId.HasValue)
            throw new BusinessRuleException("Sản phẩm đã liên kết với chỉ tiêu.");
        var item = await planItems.GetByIdAsync(product.ResearchPlanItemId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy nội dung kế hoạch.");
        var type = await productTypes.GetByIdAsync(item.ProductTypeId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy loại sản phẩm.");
        if (type.Code != "BAI_BAO")
            throw new BusinessRuleException("Chỉ bài báo mới được liên kết chỉ tiêu bài báo.");
        var plan = await plans.GetByIdAsync(item.ResearchPlanId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy kế hoạch.");
        var target = await targets.GetByIdAsync(targetId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy chỉ tiêu NCKH.");
        if (product.JournalConferenceId.HasValue && product.JournalConferenceId != target.JournalConferenceId)
            throw new BusinessRuleException("Tạp chí/hội nghị của sản phẩm không khớp chỉ tiêu.");
        await ValidateTargetLinkAsync(lecturer.Id, plan, targetId, target.JournalConferenceId, cancellationToken);
        product.LinkTarget(targetId, target.JournalConferenceId);
        await products.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(product, [], [], cancellationToken);
    }

    private async Task ValidateTargetLinkAsync(long lecturerId, ResearchPlan plan, long targetId,
        long? journalConferenceId, CancellationToken cancellationToken)
    {
            var target = await targets.GetByIdAsync(targetId, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy chỉ tiêu NCKH.");
            if (target.Status != "DA_KHOA")
                throw new BusinessRuleException("Chỉ tiêu chưa được khóa phân bổ cho bộ môn.");
            var access = await lecturerAccess.GetAccessInfoAsync(lecturerId, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy đơn vị của giảng viên.");
            if (target.AcademicYearId != plan.AcademicYearId || target.FacultyId != access.FacultyId)
                throw new BusinessRuleException("Chỉ tiêu không thuộc năm học hoặc khoa của kế hoạch.");
            var allocation = await targetAllocations.GetByTargetAndDepartmentAsync(target.Id, access.DepartmentId, cancellationToken);
            if (allocation is null)
                throw new BusinessRuleException("Bộ môn của giảng viên chưa được phân bổ chỉ tiêu này.");
            var personalTarget = await lecturerTargetAllocations.GetAllocatedArticleCountAsync(allocation.Id, lecturerId, cancellationToken);
            if (!personalTarget.HasValue)
                throw new BusinessRuleException("Giảng viên chưa được phân công thực hiện chỉ tiêu này.");
            var registeredCount = await products.CountByTargetAndLecturerAsync(target.Id, lecturerId, cancellationToken);
            if (registeredCount >= personalTarget.Value)
                throw new BusinessRuleException("Số bài đăng ký đã đạt chỉ tiêu được phân công cho giảng viên.");
            if (target.JournalConferenceId != journalConferenceId)
                throw new BusinessRuleException("Tạp chí/hội nghị của sản phẩm không khớp chỉ tiêu đã chọn.");
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
            ToStoredArticleStatus(request.ArticleStatus), request.JournalIndex, request.JournalClassification, request.ProjectLevel);
        if (request.ExternalAuthorCount.HasValue) product.SetExternalAuthorCount(request.ExternalAuthorCount.Value);
        if (request.ClassificationId.HasValue) product.ClassificationId = request.ClassificationId;

        await products.SaveChangesAsync(cancellationToken);

        var coAuthorList = await coAuthors.GetByProductIdAsync(id, cancellationToken);
        var evidenceList = await evidences.GetByProductIdAsync(id, cancellationToken);
        return await ToResponseAsync(product, coAuthorList, evidenceList, cancellationToken);
    }

    public async Task<ProductResponse> UpdateArticleProgressAsync(
        long id,
        UpdateArticleProgressRequest request,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

        await EnsureOwnerOrAdminAsync(product, cancellationToken);
        if (product.Status == ProductStatuses.Approved)
            throw new BusinessRuleException("Sản phẩm đã được duyệt hoàn tất nên không thể cập nhật tiến độ bài viết.");

        var status = ToStoredArticleStatus(request.ArticleStatus)
            ?? throw new BusinessRuleException("Trạng thái bài viết không hợp lệ.");
        var oldStatus = product.ArticleStatus;
        var oldRank = ArticleProgressRank(oldStatus);
        var newRank = ArticleProgressRank(status);
        if (product.Status is not (ProductStatuses.Draft or ProductStatuses.Returned) && newRank < oldRank)
            throw new BusinessRuleException("Sản phẩm đã gửi duyệt chỉ được cập nhật tiến độ theo chiều tiến lên.");
        if (status == "DA_XUAT_BAN" && !request.PublishedDate.HasValue)
            throw new BusinessRuleException("Bài viết đã xuất bản phải có ngày công bố.");
        var vietnamNow = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            clock.GetUtcNow(), OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");
        if (request.PublishedDate.HasValue && request.PublishedDate.Value > DateOnly.FromDateTime(vietnamNow.DateTime))
            throw new BusinessRuleException("Ngày công bố không được nằm trong tương lai.");

        try
        {
            product.UpdateArticleProgress(status, request.PublishedDate, request.PublicationInfo);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
        await reviewHistories.AddAsync(new ReviewHistory(
            product.Id,
            GetCurrentAccountId(),
            product.Status,
            product.Status,
            $"Cập nhật tiến độ bài viết: {ToPublicArticleStatus(oldStatus) ?? "chưa cập nhật"} → {ToPublicArticleStatus(status)}."), cancellationToken);
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

        var owner = await lecturers.GetByIdAsync(product.SubmittedByLecturerId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên nộp sản phẩm.");
        var ownerAccount = await accounts.GetByIdAsync(owner.AccountId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy tài khoản của giảng viên nộp sản phẩm.");
        var ownerRoles = ownerAccount.RoleAssignments.Select(x => x.Role).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var (requiredRole, approvalStatus) = ApprovalStep(product.Status, ownerRoles);

        // Kiểm tra vai trò người duyệt
        if (!currentUser.IsInRole(requiredRole) && !currentUser.IsInRole(Roles.Admin))
            throw new ForbiddenException("Bạn không có quyền xét duyệt ở bước này.");

        var access = await lecturerAccess.GetAccessInfoAsync(product.SubmittedByLecturerId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên nộp sản phẩm.");
        if (!currentUser.IsInRole(Roles.Admin) &&
            !currentUser.CanAccess(access.FacultyId, access.DepartmentId))
            throw new ForbiddenException("Bạn không có quyền xét duyệt sản phẩm ngoài đơn vị được phân công.");

        if (request.ScoreEquivalent.HasValue &&
            requiredRole != Roles.ResearchOffice &&
            !currentUser.IsInRole(Roles.Admin))
            throw new BusinessRuleException("Chỉ Phòng QLKH được nhập điểm quy đổi ở bước duyệt cuối.");

        var actorId = GetCurrentAccountId();
        var authorAccountIds = new HashSet<long> { owner.AccountId };
        foreach (var coAuthor in await coAuthors.GetByProductIdAsync(product.Id, cancellationToken))
        {
            var lecturer = await lecturers.GetByIdAsync(coAuthor.LecturerId, cancellationToken);
            if (lecturer is not null) authorAccountIds.Add(lecturer.AccountId);
        }
        if (authorAccountIds.Contains(actorId))
            throw new ForbiddenException("Bạn không được tự xét duyệt sản phẩm mà mình là tác giả hoặc đồng tác giả.");

        var fromStatus = product.Status;

        if (action == "APPROVE")
        {
            var nextStatus = approvalStatus;
            if (nextStatus == ProductStatuses.Approved)
                await EnsureReadyForFinalApprovalAsync(product, cancellationToken);
            product.Approve(nextStatus, request.ScoreEquivalent);
            if (nextStatus == ProductStatuses.Approved)
            {
                var planItem = await planItems.GetByIdAsync(product.ResearchPlanItemId, cancellationToken)
                    ?? throw new NotFoundException("Không tìm thấy nội dung kế hoạch của sản phẩm.");
                var itemProducts = await products.GetByPlanItemIdAsync(planItem.Id, cancellationToken);
                if (itemProducts.Count(x => x.Status == ProductStatuses.Approved) >= planItem.CommittedQuantity)
                    planItem.UpdateProgress("DA_THUC_HIEN", planItem.Result, planItem.DifficultyProposal);
            }
            var history = new ReviewHistory(product.Id, actorId, fromStatus, product.Status, request.Comment);
            await reviewHistories.AddAsync(history, cancellationToken);
        }
        else // REJECT
        {
            if (string.IsNullOrWhiteSpace(request.Comment))
                throw new BusinessRuleException("Phải nhập lý do khi trả lại sản phẩm.");
            product.Return();
            var history = new ReviewHistory(product.Id, actorId, fromStatus, ProductStatuses.Returned, request.Comment);
            await reviewHistories.AddAsync(history, cancellationToken);

            var reason = $"Sản phẩm đã được trả lại: {request.Comment.Trim()}";
            var notification = new Notification(
                owner.AccountId,
                "PHAN_HOI",
                reason,
                $"TRA_LAI:PRODUCT:{product.Id}:{Guid.NewGuid():N}",
                scientificProductId: product.Id,
                actionPath: $"products:{product.Id}");
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

    private static (string RequiredRole, string NextStatus) ApprovalStep(
        string status, IReadOnlySet<string> ownerRoles)
    {
        // Trưởng bộ môn không tự duyệt bước của mình: chuyển thẳng cho Trưởng khoa.
        if (status == ProductStatuses.Submitted && ownerRoles.Contains(Roles.DepartmentHead))
            return (Roles.FacultyDean, ProductStatuses.FacultyDeanApproved);

        // Trưởng khoa không tự duyệt bước của mình: chuyển thẳng cho Phòng QLKH.
        if (status == ProductStatuses.DepartmentHeadApproved && ownerRoles.Contains(Roles.FacultyDean))
            return (Roles.ResearchOffice, ProductStatuses.Approved);

        if (!ApproverRoleForStatus.TryGetValue(status, out var requiredRole) ||
            !ApproveTransitions.TryGetValue(status, out var nextStatus))
            throw new BusinessRuleException($"Sản phẩm ở trạng thái '{status}' không thể xét duyệt.");
        return (requiredRole, nextStatus);
    }

    private static int ArticleProgressRank(string? status) => status switch
    {
        "DANG_VIET" => 0,
        "DANG_PHAN_BIEN" => 1,
        "DA_NHAN_DANG" or "DA_NHAN_XET" => 2,
        "DA_XUAT_BAN" => 3,
        _ => -1
    };

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
        var responseItem = await planItems.GetByIdAsync(product.ResearchPlanItemId, cancellationToken);
        var responseType = responseItem is null ? null : await productTypes.GetByIdAsync(responseItem.ProductTypeId, cancellationToken);

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
            ToPublicArticleStatus(product.ArticleStatus),
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
            evidenceResponses,
            product.ExternalAuthorCount,
            product.ClassificationId,
            responseItem?.ProductTypeId ?? 0,
            responseType?.Code ?? "");
    }

    private static ProductSummaryResponse ToSummary(Product p) =>
        new(p.Id, p.ResearchPlanItemId, p.Title, ToPublicArticleStatus(p.ArticleStatus), p.Status, p.ScoreEquivalent, p.CreatedAt, p.UpdatedAt);

    // CSDL cũ lưu DA_NHAN_DANG. API dùng DA_NHAN_XET theo đúng thuật ngữ của đề cương,
    // đồng thời giữ tương thích với dữ liệu và ràng buộc CHECK đã triển khai.
    private static string? ToStoredArticleStatus(string? value) =>
        value == "DA_NHAN_XET" ? "DA_NHAN_DANG" : value;

    private static string? ToPublicArticleStatus(string? value) =>
        value == "DA_NHAN_DANG" ? "DA_NHAN_XET" : value;
}
