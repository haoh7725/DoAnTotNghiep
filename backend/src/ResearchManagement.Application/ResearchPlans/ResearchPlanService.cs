using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchPlans.Abstractions;
using ResearchManagement.Application.ResearchPlans.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ResearchPlans;

public sealed class ResearchPlanService(
    IResearchPlanRepository researchPlans)
{
    public async Task<ResearchPlanResponse> CreateAsync(
        CreateResearchPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Deadline.HasValue &&
            request.Deadline.Value < request.PlanDate)
        {
            throw new BusinessRuleException(
                "Hạn hoàn thành không được trước ngày lập kế hoạch.");
        }

        var exists = await researchPlans.ExistsAsync(
            request.LecturerId,
            request.AcademicYearId,
            cancellationToken);

        if (exists)
        {
            throw new ConflictException(
                "Giảng viên đã có kế hoạch NCKH trong năm học này.");
        }

        var plan = new ResearchPlan(
            request.LecturerId,
            request.AcademicYearId,
            request.CommittedProductCount,
            request.PlanDate,
            request.Deadline);

        await researchPlans.AddAsync(plan, cancellationToken);
        await researchPlans.SaveChangesAsync(cancellationToken);

        return ToResponse(plan);
    }

    public async Task<ResearchPlanResponse> GetByIdAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var plan = await researchPlans.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(
                "Không tìm thấy kế hoạch NCKH.");

        return ToResponse(plan);
    }

    public async Task<ResearchPlanResponse> UpdateAsync(
        long id,
        UpdateResearchPlanRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await researchPlans.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(
                "Không tìm thấy kế hoạch NCKH.");

        if (plan.Status == "DA_DANG_KY")
        {
            throw new BusinessRuleException(
                "Kế hoạch đã đăng ký nên không thể chỉnh sửa.");
        }

        if (request.Deadline.HasValue &&
            request.Deadline.Value < plan.PlanDate)
        {
            throw new BusinessRuleException(
                "Hạn hoàn thành không được trước ngày lập kế hoạch.");
        }

        plan.Update(
            request.CommittedProductCount,
            request.Deadline);

        await researchPlans.SaveChangesAsync(cancellationToken);

        return ToResponse(plan);
    }

    public async Task<ResearchPlanResponse> SubmitAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var plan = await researchPlans.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(
                "Không tìm thấy kế hoạch NCKH.");

        if (plan.Status == "DA_DANG_KY")
        {
            throw new ConflictException(
                "Kế hoạch đã được đăng ký trước đó.");
        }

        plan.Submit();

        await researchPlans.SaveChangesAsync(cancellationToken);

        return ToResponse(plan);
    }

    private static ResearchPlanResponse ToResponse(ResearchPlan plan) => new(
        plan.Id,
        plan.LecturerId,
        plan.AcademicYearId,
        plan.CommittedProductCount,
        plan.PlanDate,
        plan.Status,
        plan.Deadline);
}