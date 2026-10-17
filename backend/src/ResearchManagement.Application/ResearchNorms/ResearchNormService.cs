using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchNorms.Abstractions;
using ResearchManagement.Application.ResearchNorms.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ResearchNorms;

public sealed class ResearchNormService(
    IResearchNormRepository researchNorms)
{
    public async Task<IReadOnlyList<ResearchNormResponse>> GetAllAsync(
    CancellationToken cancellationToken)
    {
        var result = await researchNorms.GetAllAsync(cancellationToken);

        var lecturerIds = result
            .Select(x => x.LecturerId)
            .Distinct()
            .ToArray();

        var lecturers = await researchNorms.GetLecturerNamesAsync(
            lecturerIds,
            cancellationToken);

        return result.Select(norm =>
        {
            var response = ToResponse(norm);

            if (!lecturers.TryGetValue(norm.LecturerId, out var lecturer))
                return response;

            return response with
            {
                LecturerCode = lecturer.Code,
                LecturerName = lecturer.FullName
            };
        }).ToList();
    }

    public async Task<ResearchNormResponse> GetByIdAsync(
    long id,
    CancellationToken cancellationToken)
    {
        var researchNorm = await researchNorms.GetByIdAsync(
            id,
            cancellationToken)
            ?? throw new NotFoundException(
                "Không tìm thấy định mức NCKH.");

        var lecturers = await researchNorms.GetLecturerNamesAsync(
            new[] { researchNorm.LecturerId },
            cancellationToken);

        var response = ToResponse(researchNorm);

        if (!lecturers.TryGetValue(researchNorm.LecturerId, out var lecturer))
            return response;

        return response with
        {
            LecturerCode = lecturer.Code,
            LecturerName = lecturer.FullName
        };
    }

    public async Task<ResearchNormResponse> CreateAsync(
        CreateResearchNormRequest request,
        CancellationToken cancellationToken)
    {
        var existing =
            await researchNorms.GetByLecturerAndAcademicYearAsync(
                request.LecturerId,
                request.AcademicYearId,
                cancellationToken);

        if (existing is not null)
        {
            throw new BusinessRuleException(
                "Giảng viên đã có định mức NCKH trong năm học này.");
        }

        if (request.RequiredHours < 0)
        {
            throw new BusinessRuleException(
                "Số giờ định mức không được âm.");
        }

        var researchNorm = new ResearchNorm(
            request.LecturerId,
            request.AcademicYearId,
            request.RequiredHours,
            request.Basis);

        await researchNorms.AddAsync(
            researchNorm,
            cancellationToken);

        await researchNorms.SaveChangesAsync(
            cancellationToken);

        return ToResponse(researchNorm);
    }

    public async Task<ResearchNormResponse> UpdateAsync(
        long id,
        UpdateResearchNormRequest request,
        CancellationToken cancellationToken)
    {
        var researchNorm = await researchNorms.GetByIdAsync(
            id,
            cancellationToken)
            ?? throw new NotFoundException(
                "Không tìm thấy định mức NCKH.");

        if (request.RequiredHours < 0)
        {
            throw new BusinessRuleException(
                "Số giờ định mức không được âm.");
        }

        researchNorm.Update(
            request.RequiredHours,
            request.Basis);

        await researchNorms.SaveChangesAsync(
            cancellationToken);

        return ToResponse(researchNorm);
    }

    private static ResearchNormResponse ToResponse(
        ResearchNorm researchNorm) =>
        new(
            researchNorm.Id,
            researchNorm.LecturerId,
            researchNorm.AcademicYearId,
            researchNorm.RequiredHours,
            researchNorm.Basis);
}