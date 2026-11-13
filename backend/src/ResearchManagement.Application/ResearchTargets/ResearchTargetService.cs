using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchTargets.Abstractions;
using ResearchManagement.Application.ResearchTargets.Models;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Application.ResearchTargetAllocations.Abstractions;
using ResearchManagement.Application.Auth.Abstractions;

namespace ResearchManagement.Application.ResearchTargets;

public sealed class ResearchTargetService(
    IResearchTargetRepository researchTargets,
    IResearchTargetAllocationRepository allocations,
    ICurrentUser currentUser)
{
    public async Task<List<ResearchTargetResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var items = await researchTargets.GetAllAsync(cancellationToken);

        return items.Select(ToResponse).ToList();
    }

    public async Task<ResearchTargetResponse> GetByIdAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var item = await researchTargets.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy chỉ tiêu NCKH.");

        return ToResponse(item);
    }

    public async Task<ResearchTargetResponse> CreateAsync(
        CreateResearchTargetRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AssignedArticleCount <= 0)
            throw new BusinessRuleException("Số bài được giao phải lớn hơn 0.");
        if (request.ProductTypeId <= 0)
            throw new BusinessRuleException("Phải chọn loại sản phẩm cho chỉ tiêu.");

        var existing =
            await researchTargets.GetByFacultyAndAcademicYearAsync(
                request.FacultyId,
                request.AcademicYearId,
                request.ProductTypeId,
                request.JournalConferenceId,
                cancellationToken);

        if (existing is not null)
            throw new BusinessRuleException(
                "Khoa đã có chỉ tiêu NCKH trong năm học này.");

        var researchTarget = new ResearchTarget(
            request.FacultyId,
            request.AcademicYearId,
            request.AssignedArticleCount,
            request.Deadline,
            request.JournalConferenceId,
            request.ProductTypeId);
        researchTarget.MarkAssigned(currentUser.Id ?? throw new ForbiddenException("Không xác định được tài khoản giao chỉ tiêu."));

        await researchTargets.AddAsync(researchTarget, cancellationToken);
        await researchTargets.SaveChangesAsync(cancellationToken);

        return ToResponse(researchTarget);
    }

    public async Task<ResearchTargetResponse> UpdateAsync(
        long id,
        UpdateResearchTargetRequest request,
        CancellationToken cancellationToken)
    {
        var researchTarget =
            await researchTargets.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy chỉ tiêu NCKH.");
        if (researchTarget.Status is "DA_KHOA" or "DA_HUY")
            throw new BusinessRuleException("Chỉ tiêu đã khóa hoặc đã hủy nên không thể chỉnh sửa.");

        if (request.AssignedArticleCount <= 0)
            throw new BusinessRuleException("Số bài được giao phải lớn hơn 0.");

        if (request.ProductTypeId <= 0) throw new BusinessRuleException("Phải chọn loại sản phẩm cho chỉ tiêu.");

        var allocatedTotal = (await allocations.GetByResearchTargetIdAsync(id, cancellationToken))
            .Sum(x => x.AllocatedArticleCount);
        if (request.AssignedArticleCount < allocatedTotal)
            throw new BusinessRuleException("Chỉ tiêu mới không được nhỏ hơn tổng số bài đã phân bổ.");

        researchTarget.Update(
            request.AssignedArticleCount,
            request.Deadline,
            request.JournalConferenceId,
            request.ProductTypeId);

        await researchTargets.SaveChangesAsync(cancellationToken);

        return ToResponse(researchTarget);
    }

    private static ResearchTargetResponse ToResponse(
        ResearchTarget researchTarget)
    {
        return new ResearchTargetResponse(
            researchTarget.Id,
            researchTarget.FacultyId,
            researchTarget.AcademicYearId,
            researchTarget.ProductTypeId ?? throw new BusinessRuleException("Chỉ tiêu chưa có loại sản phẩm."),
            researchTarget.AssignedArticleCount,
            researchTarget.Deadline,
            researchTarget.JournalConferenceId,
            researchTarget.Status,
            researchTarget.AssignedByAccountId,
            researchTarget.AcceptedByAccountId,
            researchTarget.AcceptedAt,
            researchTarget.PublishedAt,
            researchTarget.LockedAt);
    }
}
