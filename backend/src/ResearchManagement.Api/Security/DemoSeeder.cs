using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;
namespace ResearchManagement.Api.Security;
public static class DemoSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var password = config["Seed:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 128)
            throw new InvalidOperationException("Set Seed__Password (12–128 characters) before seeding.");
        var db = services.GetRequiredService<ApplicationDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<Account>>();
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(1872026)");
        var faculty = await db.Faculties.SingleOrDefaultAsync(x=>x.Code=="DEMO_CNTT");
        if (faculty is null) { faculty=new Faculty("DEMO_CNTT", "Khoa Công nghệ thông tin (demo)"); db.Add(faculty); await db.SaveChangesAsync(); }
        var department = await db.Departments.SingleOrDefaultAsync(x=>x.Code=="DEMO_CNPM");
        if (department is null) { department=new Department { Code="DEMO_CNPM", Name="Bộ môn Công nghệ phần mềm (demo)", FacultyId=faculty.Id }; db.Add(department); await db.SaveChangesAsync(); }
        if (!await db.AcademicYears.AnyAsync(x=>x.Code=="2026-2027"))
            db.Add(new AcademicYear { Code="2026-2027", StartDate=new DateOnly(2026,9,1), EndDate=new DateOnly(2027,8,31) });
        string[] roles = ["QUAN_TRI", "GIANG_VIEN", "TRUONG_BO_MON", "TRUONG_KHOA", "PHONG_QLKH", "BAN_GIAM_HIEU"];
        foreach (var role in roles)
        {
            var username="demo."+role.ToLowerInvariant();
            // Never reset passwords or elevate existing accounts on rerun.
            if (await db.Accounts.AnyAsync(x=>x.Username==username)) continue;
            var account=new Account { Username=username, FullName="Demo "+role };
            account.PasswordHash=hasher.HashPassword(account,password); db.Add(account); await db.SaveChangesAsync();
            var scope=role switch { "GIANG_VIEN"=>"CA_NHAN", "TRUONG_BO_MON"=>"BO_MON", "TRUONG_KHOA"=>"KHOA", _=>"TOAN_TRUONG" };
            db.Add(new RoleAssignment { AccountId=account.Id, Role=role, Scope=scope,
                FacultyId=scope is "KHOA" or "BO_MON" ? faculty.Id : null,
                DepartmentId=scope=="BO_MON" ? department.Id : null });
            if (role is "GIANG_VIEN" or "TRUONG_BO_MON" or "TRUONG_KHOA")
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO nckh.giang_vien(bo_mon_id,tai_khoan_id,ma_giang_vien,ho_ten) VALUES ({department.Id},{account.Id},{"DEMO_"+account.Id},{account.FullName})");
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
        Console.WriteLine("Demo data ready. Six demo.<role> accounts use the supplied password when first created. Existing accounts were preserved.");
    }
}
