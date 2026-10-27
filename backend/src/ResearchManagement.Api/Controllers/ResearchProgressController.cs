using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/research-progress")]
[Authorize]
public sealed class ResearchProgressController(
    ApplicationDbContext db,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Mine(
        [FromQuery] long academicYearId,
        CancellationToken ct)
    {
        if (academicYearId <= 0)
            throw new BusinessRuleException("Vui lòng chọn năm học.");

        var accountId = currentUser.Id
            ?? throw new ForbiddenException(
                "Không xác định được tài khoản.");

        // Tự xác định giảng viên từ tài khoản đăng nhập.
        // Không nhận lecturerId từ người gọi.
        var lecturer = await db.Lecturers.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.AccountId == accountId, ct)
            ?? throw new NotFoundException(
                "Tài khoản chưa liên kết với giảng viên.");

        var year = await db.AcademicYears.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == academicYearId, ct)
            ?? throw new NotFoundException(
                "Không tìm thấy năm học.");

        var requiredHours = await db.ResearchNorms.AsNoTracking()
            .Where(x =>
                x.LecturerId == lecturer.Id &&
                x.AcademicYearId == academicYearId)
            .Select(x => (decimal?)x.RequiredHours)
            .SingleOrDefaultAsync(ct);

        // Cùng nguyên tắc với báo cáo Web:
        // lấy lần đánh giá đã chốt mới nhất.
        var evaluation = await db.Evaluations.AsNoTracking()
            .Where(x =>
                x.LecturerId == lecturer.Id &&
                x.AcademicYearId == academicYearId &&
                x.Status == "CHOT")
            .OrderByDescending(x => x.Attempt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        decimal? confirmedHours = null;
        decimal? confirmedPoints = null;
        int? confirmedProductCount = null;

        if (evaluation != null)
        {
            var details = db.EvaluationConversionDetails
                .AsNoTracking()
                .Where(x => x.EvaluationId == evaluation.Id);

            confirmedHours = await details
                .Where(x => x.Unit == "GIO")
                .SumAsync(x => x.ConvertedValue, ct);

            confirmedPoints = await details
                .Where(x => x.Unit == "DIEM")
                .SumAsync(x => x.ConvertedValue, ct);

            // Một sản phẩm có thể có nhiều dòng quy đổi.
            confirmedProductCount = await details
                .Select(x => x.ProductId)
                .Distinct()
                .CountAsync(ct);
        }

        decimal? completionPercent = null;

        if (requiredHours.HasValue &&
            requiredHours.Value > 0 &&
            confirmedHours.HasValue)
        {
            completionPercent = Math.Round(
                confirmedHours.Value * 100m /
                requiredHours.Value,
                2);
        }

        return Ok(new
        {
            AcademicYearId = year.Id,
            AcademicYearCode = year.Code,
            LecturerName = lecturer.FullName,
            RequiredHours = requiredHours,
            HasFinalizedEvaluation = evaluation != null,
            ConfirmedHours = confirmedHours,
            ConfirmedPoints = confirmedPoints,
            ConfirmedProductCount = confirmedProductCount,
            CompletionPercent = completionPercent,
            EvaluatedAt = evaluation?.EvaluatedAt
        });
    }
}
