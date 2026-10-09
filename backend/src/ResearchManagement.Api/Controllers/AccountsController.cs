using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Api.Contracts;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Security;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Controllers;

[ApiController, Route("api/admin/accounts"), Authorize(Policy = Policies.Admin)]
public sealed class AccountsController(
    ApplicationDbContext db,
    IPasswordService passwords,
    ICurrentUser currentUser) : ControllerBase
{
    private static object View(Account x) => new { x.Id, x.Username, x.FullName, x.Email, x.Status, x.CreatedAt };

    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (page is < 1 or > 1_000_000 || pageSize is < 1 or > 100) return BadRequest();
        var total = await db.Accounts.CountAsync(ct);
        var items = await db.Accounts.AsNoTracking().OrderBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.Username, x.FullName, x.Email, x.Status, x.CreatedAt }).ToListAsync(ct);
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        await db.Accounts.FindAsync([id], ct) is { } item ? Ok(View(item)) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequest request, CancellationToken ct)
    {
        var item = new Account(request.Username.Trim(), string.Empty, request.FullName.Trim(), request.Email?.Trim());
        item.SetPasswordHash(passwords.Hash(item, request.Password));
        db.Add(item);
        await db.SaveChangesAsync(ct);
        return Created($"/api/admin/accounts/{item.Id}", View(item));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateAccountRequest request, CancellationToken ct)
    {
        if (id == currentUser.Id && request.Status == AccountStatuses.Locked)
            return BadRequest(new { title = "Không thể tự khóa tài khoản đang sử dụng." });
        var item = await db.Accounts.FindAsync([id], ct);
        if (item is null) return NotFound();
        item.UpdateProfile(request.FullName.Trim(), request.Email?.Trim());
        if (request.Status == AccountStatuses.Locked) item.Lock(); else item.Unlock();
        await db.SaveChangesAsync(ct);
        return Ok(View(item));
    }

    [HttpPut("{id:long}/password")]
    public async Task<IActionResult> ResetPassword(long id, PasswordRequest request, CancellationToken ct)
    {
        var item = await db.Accounts.FindAsync([id], ct);
        if (item is null) return NotFound();
        item.SetPasswordHash(passwords.Hash(item, request.Password));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("{id:long}/permissions")]
    public async Task<IActionResult> Permissions(long id, CancellationToken ct)
    {
        if (!await db.Accounts.AnyAsync(x => x.Id == id, ct)) return NotFound();
        return Ok(await db.RoleAssignments.AsNoTracking().Where(x => x.AccountId == id).OrderBy(x => x.Id).ToListAsync(ct));
    }

    [HttpPut("{id:long}/permissions")]
    public async Task<IActionResult> SetPermissions(long id, List<PermissionRequest> requests, CancellationToken ct)
    {
        if (id == currentUser.Id)
            return BadRequest(new { title = "Nhờ quản trị viên khác thay đổi quyền của tài khoản đang sử dụng." });
        if (requests.Count > 100) return BadRequest(new { title = "Tối đa 100 quyền mỗi tài khoản." });
        var permissions = requests.Select(x => new Permission(x.Role, x.Scope, x.FacultyId, x.DepartmentId)).ToList();
        if (permissions.Any(p => !PermissionRules.IsValid(p)) || permissions.Distinct().Count() != permissions.Count)
            return BadRequest(new { title = "Vai trò và phạm vi không hợp lệ hoặc bị trùng." });

        foreach (var permission in permissions)
        {
            if (permission.FacultyId is { } facultyId && !await db.Faculties.AnyAsync(x => x.Id == facultyId, ct))
                return BadRequest(new { title = "Khoa không tồn tại." });
            if (permission.DepartmentId is { } departmentId &&
                !await db.Departments.AnyAsync(x => x.Id == departmentId && x.FacultyId == permission.FacultyId, ct))
                return BadRequest(new { title = "Bộ môn không thuộc khoa đã chọn." });
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var item = await db.Accounts.FromSqlInterpolated($"SELECT * FROM nckh.tai_khoan WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        if (item is null) return NotFound();
        await db.RoleAssignments.Where(x => x.AccountId == id).ExecuteDeleteAsync(ct);
        db.RoleAssignments.AddRange(permissions.Select(p =>
            new RoleAssignment(id, p.Role, p.Scope, p.FacultyId, p.DepartmentId)));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        if (id == currentUser.Id) return BadRequest(new { title = "Không thể tự xóa tài khoản đang sử dụng." });
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var item = await db.Accounts.FromSqlInterpolated($"SELECT * FROM nckh.tai_khoan WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        if (item is null) return NotFound();
        await db.RoleAssignments.Where(x => x.AccountId == id).ExecuteDeleteAsync(ct);
        db.Remove(item);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return NoContent();
    }
}
