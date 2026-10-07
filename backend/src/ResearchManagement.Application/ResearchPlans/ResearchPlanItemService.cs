using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchPlans.Abstractions;
using ResearchManagement.Application.ResearchPlans.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ResearchPlans;

public sealed class ResearchPlanItemService(
    IResearchPlanItemRepository items,
    IResearchPlanRepository plans,
    IProgressHistoryRepository progressHistories,
    ILecturerAccessRepository lecturerAccess,
    ICurrentUser currentUser)
{
    private static readonly HashSet<string> ValidStatuses =
    [
        "CHUA_THUC_HIEN",
        "DANG_THUC_HIEN",
        "DA_THUC_HIEN"
    ];

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

        return ToResponse(item);
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

        return result.Select(ToResponse).ToList();
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

        await EnsureCanAccessPlanAsync(plan, cancellationToken);

        if (plan.Status == "DA_DANG_KY")
        {
            throw new BusinessRuleException(
                "Kế hoạch đã đăng ký nên không thể thêm nội dung.");
        }

        var item = new ResearchPlanItem(
            researchPlanId,
            request.ProductTypeId,
            request.Name,
            request.CommittedQuantity,
            request.Deadline);

        await items.AddAsync(item, cancellationToken);
        await items.SaveChangesAsync(cancellationToken);

        return ToResponse(item);
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

        await EnsureCanAccessPlanAsync(plan, cancellationToken);

        if (plan.Status == "DA_DANG_KY")
        {
            throw new BusinessRuleException(
                "Kế hoạch đã đăng ký nên không thể chỉnh sửa nội dung.");
        }

        item.Update(
            request.Name,
            request.CommittedQuantity,
            request.Deadline);

        await items.SaveChangesAsync(cancellationToken);

        return ToResponse(item);
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

        await EnsureCanAccessPlanAsync(plan, cancellationToken);

        var status = request.Status.Trim().ToUpperInvariant();

        if (!ValidStatuses.Contains(status))
        {
            throw new BusinessRuleException(
                "Trạng thái tiến độ không hợp lệ.");
        }

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

        return ToResponse(item);
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

    private static ResearchPlanItemResponse ToResponse(
        ResearchPlanItem item) =>
        new(
            item.Id,
            item.ResearchPlanId,
            item.ProductTypeId,
            item.Name,
            item.CommittedQuantity,
            item.Deadline,
            item.Status,
            item.Result,
            item.DifficultyProposal,
            item.UpdatedAt);
}