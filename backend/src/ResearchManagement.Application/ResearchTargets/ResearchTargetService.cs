using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchTargets.Abstractions;
using ResearchManagement.Application.ResearchTargets.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ResearchTargets;

public sealed class ResearchTargetService(
    IResearchTargetRepository researchTargets)
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
        if (request.AssignedArticleCount < 0)
            throw new BusinessRuleException("Số bài được giao không được âm.");

        var existing =
            await researchTargets.GetByFacultyAndAcademicYearAsync(
                request.FacultyId,
                request.AcademicYearId,
                cancellationToken);

        if (existing is not null)
            throw new BusinessRuleException(
                "Khoa đã có chỉ tiêu NCKH trong năm học này.");

        var researchTarget = new ResearchTarget(
            request.FacultyId,
            request.AcademicYearId,
            request.AssignedArticleCount,
            request.Deadline);

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

        if (request.AssignedArticleCount < 0)
            throw new BusinessRuleException("Số bài được giao không được âm.");

        researchTarget.Update(
            request.AssignedArticleCount,
            request.Deadline);

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
            researchTarget.AssignedArticleCount,
            researchTarget.Deadline);
    }
}