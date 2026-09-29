using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Contracts;
using ResearchManagement.Api.Security;
using ResearchManagement.Application.Security;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;
namespace ResearchManagement.Api.Controllers;
[ApiController, Route("api/admin/accounts"), Authorize(Policy = "Administrator")]
public sealed class AccountsController(ApplicationDbContext db, IPasswordHasher<Account> hasher, CurrentSession session) : ControllerBase
{
    private static object View(Account x) => new { x.Id, x.Username, x.FullName, x.Email, x.Status, x.CreatedAt };
    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (page is < 1 or > 1000000 || pageSize is < 1 or > 100) return BadRequest();
        var total = await db.Accounts.CountAsync(ct);
        var items = await db.Accounts.AsNoTracking().OrderBy(x=>x.Id).Skip((page-1)*pageSize).Take(pageSize)
            .Select(x=>new { x.Id, x.Username, x.FullName, x.Email, x.Status, x.CreatedAt }).ToListAsync(ct);
        return Ok(new { items, total, page, pageSize });
    }
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct) => await db.Accounts.FindAsync([id], ct) is { } item ? Ok(View(item)) : NotFound();
    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequest r, CancellationToken ct)
    {
        var item = new Account { Username = r.Username.Trim(), FullName = r.FullName.Trim(), Email = r.Email?.Trim() };
        item.PasswordHash = hasher.HashPassword(item, r.Password); db.Add(item); await db.SaveChangesAsync(ct);
        return Created($"/api/admin/accounts/{item.Id}", View(item));
    }
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateAccountRequest r, CancellationToken ct)
    {
        if (id == session.Account.Id && r.Status == "KHOA") return BadRequest(new { title = "Không thể tự khóa tài khoản đang sử dụng." });
        var item = await db.Accounts.FindAsync([id], ct); if (item is null) return NotFound();
        item.FullName = r.FullName.Trim(); item.Email = r.Email?.Trim(); item.Status = r.Status; await db.SaveChangesAsync(ct); return Ok(View(item));
    }
    [HttpPut("{id:long}/password")]
    public async Task<IActionResult> ResetPassword(long id, PasswordRequest r, CancellationToken ct)
    {
        var item = await db.Accounts.FindAsync([id], ct); if (item is null) return NotFound();
        item.PasswordHash = hasher.HashPassword(item, r.Password); await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpGet("{id:long}/permissions")]
    public async Task<IActionResult> Permissions(long id, CancellationToken ct)
    {
        if (!await db.Accounts.AnyAsync(x=>x.Id==id, ct)) return NotFound();
        return Ok(await db.RoleAssignments.AsNoTracking().Where(x=>x.AccountId == id).OrderBy(x=>x.Id).ToListAsync(ct));
    }
    [HttpPut("{id:long}/permissions")]
    public async Task<IActionResult> SetPermissions(long id, List<PermissionRequest> requests, CancellationToken ct)
    {
        // Do not let an administrator remove their own recovery path.
        if (id == session.Account.Id) return BadRequest(new { title = "Nhờ quản trị viên khác thay đổi quyền của tài khoản đang sử dụng." });
        if (requests.Count > 100) return BadRequest(new { title = "Tối đa 100 quyền mỗi tài khoản." });
        var permissions = requests.Select(x=>new Permission(x.Role, x.Scope, x.FacultyId, x.DepartmentId)).ToList();
        if (permissions.Any(p=>!PermissionRules.IsValid(p)) || permissions.Distinct().Count()!=permissions.Count)
            return BadRequest(new { title = "Vai trò và phạm vi không hợp lệ hoặc bị trùng." });
        foreach (var p in permissions)
        {
            if (p.FacultyId is { } f && !await db.Faculties.AnyAsync(x=>x.Id==f,ct)) return BadRequest(new { title = "Khoa không tồn tại." });
            if (p.DepartmentId is { } d && !await db.Departments.AnyAsync(x=>x.Id==d && x.FacultyId==p.FacultyId,ct))
                return BadRequest(new { title = "Bộ môn không thuộc khoa đã chọn." });
        }
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Serialize replacement and deletion for this account.
        var item = await db.Accounts.FromSqlInterpolated($"SELECT * FROM nckh.tai_khoan WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (item is null) return NotFound();
        await db.RoleAssignments.Where(x=>x.AccountId==id).ExecuteDeleteAsync(ct);
        db.RoleAssignments.AddRange(permissions.Select(p=>new RoleAssignment { AccountId=id, Role=p.Role, Scope=p.Scope, FacultyId=p.FacultyId, DepartmentId=p.DepartmentId }));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return NoContent();
    }
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        if (id == session.Account.Id) return BadRequest(new { title = "Không thể tự xóa tài khoản đang sử dụng." });
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var item = await db.Accounts.FromSqlInterpolated($"SELECT * FROM nckh.tai_khoan WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (item is null) return NotFound();
        await db.RoleAssignments.Where(x=>x.AccountId==id).ExecuteDeleteAsync(ct);
        db.Remove(item); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return NoContent();
    }
}
