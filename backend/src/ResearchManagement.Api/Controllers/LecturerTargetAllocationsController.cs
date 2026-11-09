using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Controllers;

[ApiController, Route("api/lecturer-target-allocations"), Authorize]
public sealed class LecturerTargetAllocationsController(ApplicationDbContext db, ICurrentUser currentUser) : ControllerBase
{
    public sealed record SaveRequest(long DepartmentAllocationId, long LecturerId, int AllocatedArticleCount);

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var accountId = currentUser.Id ?? throw new ForbiddenException("Không xác định được tài khoản.");
        var lecturer = await db.Lecturers.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == accountId, ct)
            ?? throw new NotFoundException("Tài khoản chưa liên kết với giảng viên.");
        var rows = await (from personal in db.LecturerTargetAllocations.AsNoTracking()
                          join departmentAllocation in db.ResearchTargetAllocations.AsNoTracking() on personal.DepartmentAllocationId equals departmentAllocation.Id
                          join target in db.ResearchTargets.AsNoTracking() on departmentAllocation.ResearchTargetId equals target.Id
                          join year in db.AcademicYears.AsNoTracking() on target.AcademicYearId equals year.Id
                          where personal.LecturerId == lecturer.Id
                          orderby year.StartDate descending
                          select new { personal.Id, TargetId = target.Id, year.Code, personal.AllocatedArticleCount,
                              ApprovedArticles = db.Products.Count(x => x.ResearchTargetId == target.Id && x.SubmittedByLecturerId == lecturer.Id && x.Status == "DA_DUYET" && x.ArticleStatus == "DA_XUAT_BAN"),
                              personal.AllocationDate, target.Deadline, target.Status }).ToListAsync(ct);
        return Ok(rows.Select(x => new { x.Id, x.TargetId, AcademicYearCode = x.Code, x.AllocatedArticleCount, x.ApprovedArticles,
            CompletionPercent = Math.Round(x.ApprovedArticles * 100m / x.AllocatedArticleCount, 2),
            IsCompleted = x.ApprovedArticles >= x.AllocatedArticleCount, x.AllocationDate, x.Deadline, x.Status }));
    }

    [HttpGet]
    [Authorize(Roles = "TRUONG_BO_MON,TRUONG_KHOA,PHONG_QLKH,QUAN_TRI")]
    public async Task<IActionResult> List([FromQuery] long departmentAllocationId, CancellationToken ct)
    {
        var allocation = await GetAllocation(departmentAllocationId, ct);
        EnsureAccess(allocation);
        var rows = await (from item in db.LecturerTargetAllocations.AsNoTracking()
                          join lecturer in db.Lecturers.AsNoTracking() on item.LecturerId equals lecturer.Id
                          where item.DepartmentAllocationId == departmentAllocationId
                          orderby lecturer.FullName
                          select new { item.Id, item.DepartmentAllocationId, item.LecturerId, LecturerCode = lecturer.Code,
                              LecturerName = lecturer.FullName, item.AllocatedArticleCount, item.AllocationDate }).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("options")]
    [Authorize(Roles = "TRUONG_BO_MON,TRUONG_KHOA,PHONG_QLKH,QUAN_TRI")]
    public async Task<IActionResult> Options([FromQuery] long departmentAllocationId, CancellationToken ct)
    {
        var allocation = await GetAllocation(departmentAllocationId, ct);
        EnsureAccess(allocation);
        return Ok(await db.Lecturers.AsNoTracking().Where(x => x.DepartmentId == allocation.DepartmentId)
            .OrderBy(x => x.FullName).Select(x => new { x.Id, x.Code, x.FullName }).ToListAsync(ct));
    }

    [HttpPost]
    [Authorize(Roles = "TRUONG_BO_MON,TRUONG_KHOA,QUAN_TRI")]
    public async Task<IActionResult> Create(SaveRequest request, CancellationToken ct)
    {
        var allocation = await GetAllocation(request.DepartmentAllocationId, ct);
        EnsureAccess(allocation);
        var lecturer = await db.Lecturers.SingleOrDefaultAsync(x => x.Id == request.LecturerId, ct)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");
        if (lecturer.DepartmentId != allocation.DepartmentId) throw new BusinessRuleException("Giảng viên không thuộc bộ môn được phân bổ.");
        if (await db.LecturerTargetAllocations.AnyAsync(x => x.DepartmentAllocationId == allocation.Id && x.LecturerId == lecturer.Id, ct))
            throw new BusinessRuleException("Giảng viên đã được giao trong chỉ tiêu này.");
        await EnsureTotal(allocation, request.AllocatedArticleCount, null, ct);
        var item = new LecturerTargetAllocation(allocation.Id, lecturer.Id, request.AllocatedArticleCount, CurrentVietnamDate());
        db.LecturerTargetAllocations.Add(item);
        var notification = new Notification(lecturer.AccountId, "HE_THONG",
            $"Bạn được giao {request.AllocatedArticleCount} bài trong chỉ tiêu NCKH #{allocation.ResearchTargetId}.",
            $"PHAN_CONG_GV:{allocation.Id}:{lecturer.Id}:{request.AllocatedArticleCount}");
        notification.MarkAsSent(); db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);
        return Created($"api/lecturer-target-allocations/{item.Id}", new { item.Id });
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "TRUONG_BO_MON,TRUONG_KHOA,QUAN_TRI")]
    public async Task<IActionResult> Update(long id, SaveRequest request, CancellationToken ct)
    {
        var item = await db.LecturerTargetAllocations.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Không tìm thấy phân công.");
        var allocation = await GetAllocation(item.DepartmentAllocationId, ct); EnsureAccess(allocation);
        if (await HasProducts(allocation.ResearchTargetId, item.LecturerId, ct)) throw new BusinessRuleException("Không thể sửa vì giảng viên đã phát sinh sản phẩm.");
        await EnsureTotal(allocation, request.AllocatedArticleCount, id, ct);
        item.Update(request.AllocatedArticleCount, CurrentVietnamDate()); await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "TRUONG_BO_MON,TRUONG_KHOA,QUAN_TRI")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var item = await db.LecturerTargetAllocations.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Không tìm thấy phân công.");
        var allocation = await GetAllocation(item.DepartmentAllocationId, ct); EnsureAccess(allocation);
        if (await HasProducts(allocation.ResearchTargetId, item.LecturerId, ct)) throw new BusinessRuleException("Không thể hủy vì giảng viên đã phát sinh sản phẩm.");
        db.LecturerTargetAllocations.Remove(item); await db.SaveChangesAsync(ct); return NoContent();
    }

    private async Task<ResearchTargetAllocation> GetAllocation(long id, CancellationToken ct) =>
        await db.ResearchTargetAllocations.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Không tìm thấy phân bổ bộ môn.");
    private void EnsureAccess(ResearchTargetAllocation x) { if (!currentUser.CanAccess(x.FacultyId, x.DepartmentId)) throw new ForbiddenException("Không có quyền phân công cho bộ môn này."); }
    private async Task EnsureTotal(ResearchTargetAllocation a, int count, long? excludingId, CancellationToken ct)
    {
        if (count <= 0) throw new BusinessRuleException("Số bài giao phải lớn hơn 0.");
        var used = await db.LecturerTargetAllocations.Where(x => x.DepartmentAllocationId == a.Id && (!excludingId.HasValue || x.Id != excludingId.Value)).SumAsync(x => x.AllocatedArticleCount, ct);
        if (used + count > a.AllocatedArticleCount) throw new BusinessRuleException("Tổng giao cho giảng viên vượt chỉ tiêu của bộ môn.");
    }
    private Task<bool> HasProducts(long targetId, long lecturerId, CancellationToken ct) => db.Products.AnyAsync(x => x.ResearchTargetId == targetId && x.SubmittedByLecturerId == lecturerId, ct);
    private static DateOnly CurrentVietnamDate() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh").DateTime);
}
