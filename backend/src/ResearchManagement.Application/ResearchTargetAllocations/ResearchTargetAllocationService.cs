using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchTargetAllocations.Abstractions;
using ResearchManagement.Application.ResearchTargetAllocations.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ResearchTargetAllocations;

public sealed class ResearchTargetAllocationService(
    IResearchTargetAllocationRepository allocations)
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
        if (request.AllocatedArticleCount < 0)
            throw new BusinessRuleException("Số bài phân bổ không được âm.");

        var existing = await allocations.GetByTargetAndDepartmentAsync(
            request.ResearchTargetId,
            request.DepartmentId,
            cancellationToken);

        if (existing is not null)
            throw new BusinessRuleException(
                "Bộ môn đã được phân bổ chỉ tiêu này.");

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

        if (request.AllocatedArticleCount < 0)
            throw new BusinessRuleException("Số bài phân bổ không được âm.");

        allocation.Update(
            request.AllocatedArticleCount,
            request.AllocationDate);

        await allocations.SaveChangesAsync(cancellationToken);

        return ToResponse(allocation);
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