using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchPlans.Abstractions;
using ResearchManagement.Application.ResearchPlans.Models;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Application.ProductTypes.Abstractions;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Application.ResearchPlans;

public sealed class ResearchPlanItemService(
    IResearchPlanItemRepository items,
    IResearchPlanRepository plans,
    IProgressHistoryRepository progressHistories,
    IProductTypeRepository productTypes,
    IProductRepository products,
    ILecturerAccessRepository lecturerAccess,
    ICurrentUser currentUser)
{
    public async Task<ResearchPlanItemResponse> GetByIdAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy nội dung kế hoạch.");

        var plan = await plans.GetByIdAsync(
            item.ResearchPlanId,
            cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy kế hoạch NCKH.");

        await EnsureCanAccessPlanAsync(plan, cancellationToken);

        return await ToResponseAsync(item, cancellationToken);
    }

    public async Task<IReadOnlyList<ResearchPlanItemResponse>> GetByResearchPlanIdAsync(
        long researchPlanId,
        CancellationToken cancellationToken)
    {
        var plan = await plans.GetByIdAsync(
            researchPlanId,
            cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy kế hoạch NCKH.");

        await EnsureCanAccessPlanAsync(plan, cancellationToken);

        var result = await items.GetByResearchPlanIdAsync(
            researchPlanId,
            cancellationToken);

        var responses = new List<ResearchPlanItemResponse>(result.Count);
        foreach (var item in result)
            responses.Add(await ToResponseAsync(item, cancellationToken));
        return responses;
    }

    public async Task<ResearchPlanItemResponse> CreateAsync(
        long researchPlanId,
        CreateResearchPlanItemRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await plans.GetByIdAsync(
            researchPlanId,
            cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy kế hoạch NCKH.");

        await EnsureOwnerPlanAsync(plan, cancellationToken);

        if (plan.Status == "DA_DANG_KY")
        {
            throw new BusinessRuleException(
                "Kế hoạch đã đăng ký nên không thể thêm nội dung.");
        }
        if (await productTypes.GetByIdAsync(request.ProductTypeId, cancellationToken) is null)
            throw new NotFoundException("Không tìm thấy loại sản phẩm khoa học.");
        if (request.Deadline.HasValue && plan.Deadline.HasValue && request.Deadline.Value > plan.Deadline.Value)
            throw new BusinessRuleException("Hạn nội dung không được sau hạn chung của kế hoạch.");

        var item = new ResearchPlanItem(
            researchPlanId,
            request.ProductTypeId,
            request.Name,
            request.CommittedQuantity,
            request.Deadline);

        await items.AddAsync(item, cancellationToken);
        await items.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(item, cancellationToken);
    }

    public async Task<ResearchPlanItemResponse> UpdateAsync(
        long id,
        UpdateResearchPlanItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy nội dung kế hoạch.");

        var plan = await plans.GetByIdAsync(
            item.ResearchPlanId,
            cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy kế hoạch NCKH.");

        await EnsureOwnerPlanAsync(plan, cancellationToken);

        if (plan.Status == "DA_DANG_KY")
        {
            throw new BusinessRuleException(
                "Kế hoạch đã đăng ký nên không thể chỉnh sửa nội dung.");
        }
        if (request.Deadline.HasValue && plan.Deadline.HasValue && request.Deadline.Value > plan.Deadline.Value)
            throw new BusinessRuleException("Hạn nội dung không được sau hạn chung của kế hoạch.");
        var existingProducts = await products.GetByPlanItemIdAsync(item.Id, cancellationToken);
        if (request.CommittedQuantity < existingProducts.Count)
            throw new BusinessRuleException(
                $"Không thể giảm cam kết xuống {request.CommittedQuantity} vì đã có {existingProducts.Count} sản phẩm liên kết với nội dung này.");

        item.Update(
            request.Name,
            request.CommittedQuantity,
            request.Deadline);

        await items.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(item, cancellationToken);
    }

    public async Task<IReadOnlyList<ProgressHistoryResponse>> GetProgressHistoryAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy nội dung kế hoạch.");

        var plan = await plans.GetByIdAsync(
            item.ResearchPlanId,
            cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy kế hoạch NCKH.");

        await EnsureCanAccessPlanAsync(plan, cancellationToken);

        var histories = await progressHistories.GetByResearchPlanItemIdAsync(
            id,
            cancellationToken);

        return histories
            .Select(history => new ProgressHistoryResponse(
                history.Id,
                history.ResearchPlanItemId,
                history.UpdatedByAccountId,
                history.Status,
                history.Result,
                history.DifficultyProposal,
                history.UpdatedAt))
            .ToList();
    }

    public async Task<ResearchPlanItemResponse> UpdateProgressAsync(
        long id,
        UpdateResearchPlanItemProgressRequest request,
        CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy nội dung kế hoạch.");

        var plan = await plans.GetByIdAsync(
            item.ResearchPlanId,
            cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy kế hoạch NCKH.");

        await EnsureOwnerPlanAsync(plan, cancellationToken);

        var (status, _, _) = await GetDerivedProgressAsync(item, cancellationToken);

        var accountId = currentUser.Id
            ?? throw new AuthenticationFailedException(
                "Không xác định được người dùng hiện tại.");

        item.UpdateProgress(
            status,
            request.Result,
            request.DifficultyProposal);

        var history = new ProgressHistory(
            item.Id,
            accountId,
            status,
            request.Result,
            request.DifficultyProposal);

        await progressHistories.AddAsync(
            history,
            cancellationToken);

        await items.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(item, cancellationToken);
    }

    private async Task EnsureCanAccessPlanAsync(
        ResearchPlan plan,
        CancellationToken cancellationToken)
    {
        var lecturer = await lecturerAccess.GetAccessInfoAsync(
            plan.LecturerId,
            cancellationToken)
            ?? throw new NotFoundException(
                "Không tìm thấy giảng viên.");

        var accountId = currentUser.Id
            ?? throw new AuthenticationFailedException(
                "Không xác định được người dùng hiện tại.");

        var isOwner = lecturer.AccountId == accountId;

        var hasScopeAccess = currentUser.CanAccess(
            lecturer.FacultyId,
            lecturer.DepartmentId);

        if (!isOwner && !hasScopeAccess)
        {
            throw new ForbiddenException(
                "Bạn không có quyền truy cập dữ liệu của giảng viên này.");
        }
    }

    private async Task EnsureOwnerPlanAsync(ResearchPlan plan, CancellationToken cancellationToken)
    {
        var lecturer = await lecturerAccess.GetAccessInfoAsync(plan.LecturerId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");
        var accountId = currentUser.Id
            ?? throw new AuthenticationFailedException("Không xác định được người dùng hiện tại.");
        if (lecturer.AccountId != accountId)
            throw new ForbiddenException("Chỉ giảng viên chính chủ được thay đổi kế hoạch và tiến độ.");
    }

    private async Task<(string Status, int Actual, int Completed)> GetDerivedProgressAsync(
        ResearchPlanItem item, CancellationToken cancellationToken)
    {
        var actualProducts = await products.GetByPlanItemIdAsync(item.Id, cancellationToken);
        var completed = actualProducts.Count(x => x.Status == ProductStatuses.Approved);
        var status = actualProducts.Count == 0
            ? "CHUA_THUC_HIEN"
            : completed >= item.CommittedQuantity ? "DA_THUC_HIEN" : "DANG_THUC_HIEN";
        return (status, actualProducts.Count, completed);
    }

    private async Task<ResearchPlanItemResponse> ToResponseAsync(
        ResearchPlanItem item, CancellationToken cancellationToken)
    {
        var (status, actual, completed) = await GetDerivedProgressAsync(item, cancellationToken);
        return new(
            item.Id,
            item.ResearchPlanId,
            item.ProductTypeId,
            item.Name,
            item.CommittedQuantity,
            item.Deadline,
            status,
            item.Result,
            item.DifficultyProposal,
            item.UpdatedAt,
            actual,
            completed);
    }
}
