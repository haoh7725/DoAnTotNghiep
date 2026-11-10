using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchTargetAllocations;
using ResearchManagement.Application.ResearchTargetAllocations.Models;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Infrastructure.Persistence;
namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/research-target-allocations")]
[Authorize]
public sealed class ResearchTargetAllocationsController(
    ResearchTargetAllocationService service,
    ApplicationDbContext db,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("{id:long}")]
    [Authorize(Policy = Policies.Management)]
    public async Task<ActionResult<ResearchTargetAllocationResponse>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        EnsureCanRead(result.FacultyId, result.DepartmentId);
        if (!currentUser.CanAccess(result.FacultyId, null))
        {
            var status = await db.ResearchTargets.AsNoTracking()
                .Where(x => x.Id == result.ResearchTargetId).Select(x => x.Status).SingleAsync(cancellationToken);
            if (status != "DA_KHOA")
                throw new ForbiddenException("Phân bổ bộ môn chỉ được xem sau khi Trưởng khoa khóa phân bổ.");
        }
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = Policies.Management)]
    public async Task<ActionResult<List<ResearchTargetAllocationResponse>>> GetByResearchTarget(
        [FromQuery] long researchTargetId,
        CancellationToken cancellationToken)
    {
        var target = await db.ResearchTargets.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == researchTargetId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy chỉ tiêu NCKH.");
        var result = await service.GetByResearchTargetIdAsync(
            researchTargetId,
            cancellationToken);

        if (currentUser.CanAccess(target.FacultyId, null))
            return Ok(result);

        if (target.Status != "DA_KHOA")
            return Ok(new List<ResearchTargetAllocationResponse>());

        return Ok(result.Where(x => currentUser.CanAccess(x.FacultyId, x.DepartmentId)).ToList());
    }

    [HttpPost]
    [Authorize(Roles = "TRUONG_KHOA,QUAN_TRI")]
    public async Task<ActionResult<ResearchTargetAllocationResponse>> Create(
        CreateResearchTargetAllocationRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var allocationDate = CurrentVietnamDate();
        request = request with { AllocationDate = allocationDate };
        await EnsureValidAllocationAsync(request.ResearchTargetId, request.FacultyId, request.DepartmentId, allocationDate, cancellationToken);

        var result = await service.CreateAsync(request, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "TRUONG_KHOA,QUAN_TRI")]
    public async Task<ActionResult<ResearchTargetAllocationResponse>> Update(
        long id,
        UpdateResearchTargetAllocationRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var existing = await service.GetByIdAsync(id, cancellationToken);
        EnsureCanRead(existing.FacultyId, existing.DepartmentId);
        var allocationDate = CurrentVietnamDate();
        request = request with { AllocationDate = allocationDate };
        await EnsureValidAllocationAsync(existing.ResearchTargetId, existing.FacultyId, existing.DepartmentId, allocationDate, cancellationToken);

        var result = await service.UpdateAsync(
            id,
            request,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(result);
    }

    private void EnsureCanRead(long facultyId, long departmentId)
    {
        if (!currentUser.CanAccess(facultyId, departmentId))
            throw new ForbiddenException("Không có quyền truy cập phân bổ chỉ tiêu này.");
    }

    private async Task EnsureValidAllocationAsync(
        long researchTargetId,
        long facultyId,
        long departmentId,
        DateOnly allocationDate,
        CancellationToken cancellationToken)
    {
        var target = await db.ResearchTargets.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == researchTargetId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy chỉ tiêu NCKH.");
        var department = await db.Departments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == departmentId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy bộ môn.");

        if (target.FacultyId != facultyId || department.FacultyId != facultyId)
            throw new BusinessRuleException("Khoa của chỉ tiêu và bộ môn phân bổ phải trùng khớp.");
        if (target.Status != "CONG_BO")
            throw new BusinessRuleException("Chỉ được tạo hoặc sửa phân bổ bộ môn khi chỉ tiêu đang ở trạng thái công bố và chưa khóa.");
        if (!currentUser.CanAccess(facultyId, departmentId))
            throw new ForbiddenException("Không có quyền phân bổ chỉ tiêu cho bộ môn này.");
        var year = await db.AcademicYears.AsNoTracking()
            .SingleAsync(x => x.Id == target.AcademicYearId, cancellationToken);
        if (allocationDate < year.StartDate || allocationDate > year.EndDate)
            throw new BusinessRuleException("Ngày phân bổ phải nằm trong năm học của chỉ tiêu.");
        var today = CurrentVietnamDate();
        if (allocationDate < today)
            throw new BusinessRuleException("Ngày phân bổ không được nằm trong quá khứ.");
        if (target.Deadline.HasValue && allocationDate > target.Deadline.Value)
            throw new BusinessRuleException("Chỉ tiêu đã quá hạn hoàn thành; hãy điều chỉnh hạn trước khi phân bổ.");
    }

    private static DateOnly CurrentVietnamDate() =>
        DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                DateTimeOffset.UtcNow,
                OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh").DateTime);
}
