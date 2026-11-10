using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchTargetAllocations.Abstractions;
using ResearchManagement.Application.ResearchTargetAllocations.Models;
using ResearchManagement.Application.ResearchTargets.Abstractions;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ResearchTargetAllocations;

public sealed class ResearchTargetAllocationService(
    IResearchTargetAllocationRepository allocations,
    IResearchTargetRepository targets)
{
    public async Task<List<ResearchTargetAllocationResponse>> GetByResearchTargetIdAsync(
        long researchTargetId,
        CancellationToken cancellationToken)
    {
        var items = await allocations.GetByResearchTargetIdAsync(
            researchTargetId,
            cancellationToken);

        return items.Select(ToResponse).ToList();
    }

    public async Task<ResearchTargetAllocationResponse> GetByIdAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var allocation = await allocations.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phân bổ chỉ tiêu.");

        return ToResponse(allocation);
    }

    public async Task<ResearchTargetAllocationResponse> CreateAsync(
        CreateResearchTargetAllocationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AllocatedArticleCount <= 0)
            throw new BusinessRuleException("Số bài phân bổ phải lớn hơn 0.");

        var existing = await allocations.GetByTargetAndDepartmentAsync(
            request.ResearchTargetId,
            request.DepartmentId,
            cancellationToken);

        if (existing is not null)
            throw new BusinessRuleException(
                "Bộ môn đã được phân bổ chỉ tiêu này.");

        await EnsureTotalDoesNotExceedTargetAsync(
            request.ResearchTargetId,
            request.AllocatedArticleCount,
            null,
            cancellationToken);

        var allocation = new ResearchTargetAllocation(
            request.ResearchTargetId,
            request.FacultyId,
            request.DepartmentId,
            request.AllocatedArticleCount,
            request.AllocationDate);

        await allocations.AddAsync(allocation, cancellationToken);
        await allocations.SaveChangesAsync(cancellationToken);

        return ToResponse(allocation);
    }

    public async Task<ResearchTargetAllocationResponse> UpdateAsync(
        long id,
        UpdateResearchTargetAllocationRequest request,
        CancellationToken cancellationToken)
    {
        var allocation = await allocations.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phân bổ chỉ tiêu.");

        if (request.AllocatedArticleCount <= 0)
            throw new BusinessRuleException("Số bài phân bổ phải lớn hơn 0.");

        await EnsureTotalDoesNotExceedTargetAsync(
            allocation.ResearchTargetId,
            request.AllocatedArticleCount,
            allocation.Id,
            cancellationToken);

        allocation.Update(
            request.AllocatedArticleCount,
            request.AllocationDate);

        await allocations.SaveChangesAsync(cancellationToken);

        return ToResponse(allocation);
    }

    private async Task EnsureTotalDoesNotExceedTargetAsync(
        long researchTargetId,
        int requestedCount,
        long? allocationId,
        CancellationToken cancellationToken)
    {
        var target = await targets.GetByIdAsync(researchTargetId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy chỉ tiêu NCKH.");
        if (target.Status != "CONG_BO")
            throw new BusinessRuleException("Chỉ được phân bổ khi chỉ tiêu đã công bố và chưa khóa.");
        var items = await allocations.GetByResearchTargetIdAsync(researchTargetId, cancellationToken);
        var total = items.Where(x => x.Id != allocationId).Sum(x => x.AllocatedArticleCount) + requestedCount;
        if (total > target.AssignedArticleCount)
            throw new BusinessRuleException("Tổng số bài phân bổ không được vượt chỉ tiêu của khoa.");
    }

    private static ResearchTargetAllocationResponse ToResponse(
        ResearchTargetAllocation allocation)
        => new(
            allocation.Id,
            allocation.ResearchTargetId,
            allocation.FacultyId,
            allocation.DepartmentId,
            allocation.AllocatedArticleCount,
            allocation.AllocationDate);
}
