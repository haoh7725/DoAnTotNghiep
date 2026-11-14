using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Infrastructure;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Controllers;

[ApiController, Route("api/annual-research"), Authorize]
public sealed class AnnualResearchController(ApplicationDbContext db, ICurrentUser user, AnnualResearchService calculator) : ControllerBase
{
    private async Task<Lecturer> Lecturer(long id, CancellationToken ct)
    {
        var l = await db.Lecturers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");
        var faculty = await db.Departments.Where(x => x.Id == l.DepartmentId).Select(x => x.FacultyId).SingleAsync(ct);
        if (l.AccountId != user.Id && !user.CanAccess(faculty, l.DepartmentId))
            throw new ForbiddenException("Không có quyền xem giảng viên này.");
        return l;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Mine(long academicYearId, CancellationToken ct)
    {
        var id = await db.Lecturers.Where(x => x.AccountId == user.Id).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Tài khoản chưa liên kết giảng viên.");
        return Ok(await calculator.Calculate(id, academicYearId, ct));
    }

    [HttpGet("{lecturerId:long}")]
    public async Task<IActionResult> Detail(long lecturerId, long academicYearId, CancellationToken ct)
    {
        await Lecturer(lecturerId, ct);
        return Ok(await calculator.Calculate(lecturerId, academicYearId, ct));
    }

    [HttpGet, Authorize(Policy = Policies.Management)]
    public async Task<IActionResult> List(long academicYearId, CancellationToken ct)
    {
        var candidates = await (from l in db.Lecturers.AsNoTracking() join d in db.Departments on l.DepartmentId equals d.Id
            select new { Lecturer = l, d.FacultyId }).ToListAsync(ct);
        var results = new List<AnnualResearchService.Result>();
        foreach (var row in candidates.Where(x => user.CanAccess(x.FacultyId, x.Lecturer.DepartmentId)))
            results.Add(await calculator.Calculate(row.Lecturer.Id, academicYearId, ct));
        return Ok(results.OrderByDescending(x => x.TotalHours).ThenBy(x => x.LecturerCode));
    }

    public sealed record FinalizeRequest(long AcademicYearId);
    [HttpPost("{lecturerId:long}/finalize"), Authorize(Policy = Policies.ResearchOffice)]
    public async Task<IActionResult> Finalize(long lecturerId, FinalizeRequest request, CancellationToken ct)
    {
        var l = await Lecturer(lecturerId, ct);
        var facultyId = await db.Departments.Where(x => x.Id == l.DepartmentId).Select(x => x.FacultyId).SingleAsync(ct);
        if (!user.IsInRole("QUAN_TRI") && !user.Assignments.Any(a => a.Role == "TRUONG_KHOA" && a.FacultyId == facultyId))
            throw new ForbiddenException("Chỉ Trưởng khoa quản lý giảng viên này được chốt kết quả.");
        if (l.AccountId == user.Id) throw new ForbiddenException("Không được tự chốt kết quả của mình. Cần người duyệt thay thế.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var result = await calculator.Calculate(lecturerId, request.AcademicYearId, ct);
        if (result.Issues.Count > 0) throw new BusinessRuleException(string.Join("; ", result.Issues));
        var attempt = (await db.Evaluations.Where(x => x.LecturerId == lecturerId && x.AcademicYearId == request.AcademicYearId).MaxAsync(x => (int?)x.Attempt, ct) ?? 0) + 1;
        var e = new Evaluation(l.Id, request.AcademicYearId, attempt, user.Id!.Value, l.Code, l.FullName,
            l.DepartmentId, l.AcademicRank, l.Degree, "GIO");
        e.Finalize(result.Classification, $"Tổng {result.TotalHours:0.####}/586 giờ; hoàn thành {result.CompletedTargets}/{result.AssignedTargets} chỉ tiêu. Hai kết quả được xét độc lập.");
        db.Evaluations.Add(e);
        await db.SaveChangesAsync(ct);
        foreach (var line in result.Products)
            db.EvaluationConversionDetails.Add(new(e.Id, l.Id, request.AcademicYearId, line.ProductId, line.TypeId, line.RuleId,
                line.Coefficient == 0 ? 0 : line.Hours / line.Coefficient, line.Coefficient, "GIO", line.Basis));
        db.Notifications.Add(new Notification(l.AccountId, "HE_THONG", $"Kết quả NCKH đã chốt: {result.Classification}, {result.TotalHours:0.####} giờ.",
            $"ANNUAL_FINALIZED:{e.Id}", actionPath: "evaluations"));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(new { e.Id, Result = result });
    }

    [HttpGet("nominations"), Authorize(Policy = Policies.ResearchOffice)]
    public async Task<IActionResult> Nominations(long academicYearId, long facultyId, CancellationToken ct = default)
    {
        if (!user.CanAccess(facultyId, null)) throw new ForbiddenException("Không có quyền đề xuất danh hiệu cho khoa này.");
        var lecturers = await (from l in db.Lecturers join d in db.Departments on l.DepartmentId equals d.Id
            where d.FacultyId == facultyId select l).ToListAsync(ct);
        var rows = new List<AnnualResearchService.Result>();
        foreach (var l in lecturers) rows.Add(await calculator.Calculate(l.Id, academicYearId, ct));
        var proposal = AnnualAwardProposal.Build(rows);
        return Ok(new
        {
            proposal.Population,
            proposal.Eligible,
            proposal.AdvancedLaborQuota,
            proposal.TopThirtyQuota,
            proposal.EmulationFighterQuota,
            Policy = "Chỉ hiển thị, không lưu. Top 10% tổng giờ là Lao động tiên tiến; từ sau top 10% đến hết top 30% là Chiến sĩ thi đua. Chỉ xét người đạt ít nhất 586 giờ; số suất làm tròn lên. Đồng giờ tại ranh giới được cảnh báo.",
            proposal.Candidates
        });
    }
}
