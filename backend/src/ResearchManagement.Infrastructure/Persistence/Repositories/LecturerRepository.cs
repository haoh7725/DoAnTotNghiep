using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Application.Lecturers.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class LecturerRepository(ApplicationDbContext db) : ILecturerRepository
{
    private sealed class Row
    {
        public Lecturer L { get; set; } = null!;
        public Department D { get; set; } = null!;
        public Faculty F { get; set; } = null!;
    }

    private static readonly Expression<Func<Row, LecturerResponse>> ToView = x => new LecturerResponse(
        x.L.Id, x.L.AccountId, x.L.Code, x.L.FullName, x.L.BirthDate, x.L.Gender, x.L.Email, x.L.Phone,
        x.L.AcademicRank, x.L.Degree, x.L.Position, x.D.Id, x.D.Name, x.F.Id, x.F.Name);

    private IQueryable<Row> Rows() =>
        from l in db.Lecturers.AsNoTracking()
        join d in db.Departments.AsNoTracking() on l.DepartmentId equals d.Id
        join f in db.Faculties.AsNoTracking() on d.FacultyId equals f.Id
        select new Row { L = l, D = d, F = f };

    public Task<LecturerResponse?> GetViewAsync(long id, CancellationToken cancellationToken) =>
        Rows().Where(x => x.L.Id == id).Select(ToView).FirstOrDefaultAsync(cancellationToken);

    public Task<LecturerResponse?> GetViewByAccountIdAsync(long accountId, CancellationToken cancellationToken) =>
        Rows().Where(x => x.L.AccountId == accountId).Select(ToView).FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<LecturerResponse>> SearchAsync(
        LecturerSearchQuery query, LecturerVisibility visibility, CancellationToken cancellationToken)
    {
        var all = visibility.All;
        var facultyIds = visibility.FacultyIds.ToArray();
        var departmentIds = visibility.DepartmentIds.ToArray();
        var own = visibility.OwnAccountId;

        var rows = Rows().Where(x =>
            all || facultyIds.Contains(x.F.Id) || departmentIds.Contains(x.D.Id) ||
            (own != null && x.L.AccountId == own));

        if (query.FacultyId is { } facultyId) rows = rows.Where(x => x.F.Id == facultyId);
        if (query.DepartmentId is { } departmentId) rows = rows.Where(x => x.D.Id == departmentId);
        if (CleanKeyword(query.Keyword) is { } keyword)
        {
            var pattern = "%" + keyword.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            rows = rows.Where(x => EF.Functions.ILike(x.L.FullName, pattern) || EF.Functions.ILike(x.L.Code, pattern));
        }

        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderBy(x => x.L.Code).ThenBy(x => x.L.Id)
            .Skip(query.Skip).Take(query.Take)
            .Select(ToView)
            .ToListAsync(cancellationToken);

        return new PagedResult<LecturerResponse>(items, query.Page, query.Take, total);
    }

    private static string? CleanKeyword(string? keyword)
    {
        var trimmed = keyword?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public Task<Lecturer?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        db.Lecturers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Lecturer?> GetByAccountIdAsync(long accountId, CancellationToken cancellationToken) =>
        db.Lecturers.FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);

    public Task<bool> DepartmentExistsAsync(long departmentId, CancellationToken cancellationToken) =>
        db.Departments.AnyAsync(x => x.Id == departmentId, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, long? excludeId, CancellationToken cancellationToken) =>
        db.Lecturers.AnyAsync(x => x.Code == code && (excludeId == null || x.Id != excludeId), cancellationToken);

    public Task<bool> ExistsByAccountIdAsync(long accountId, CancellationToken cancellationToken) =>
        db.Lecturers.AnyAsync(x => x.AccountId == accountId, cancellationToken);

    public async Task AddAsync(Lecturer lecturer, CancellationToken cancellationToken) =>
        await db.Lecturers.AddAsync(lecturer, cancellationToken);

    public async Task RemoveAsync(Lecturer lecturer, CancellationToken cancellationToken)
    {
        var profile = await db.ScientificProfiles
            .FirstOrDefaultAsync(x => x.LecturerId == lecturer.Id, cancellationToken);
        if (profile is not null) db.ScientificProfiles.Remove(profile);
        db.Lecturers.Remove(lecturer);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

public sealed class ScientificProfileRepository(ApplicationDbContext db) : IScientificProfileRepository
{
    public Task<ScientificProfile?> GetByLecturerIdAsync(long lecturerId, CancellationToken cancellationToken) =>
        db.ScientificProfiles.FirstOrDefaultAsync(x => x.LecturerId == lecturerId, cancellationToken);

    public async Task AddAsync(ScientificProfile profile, CancellationToken cancellationToken) =>
        await db.ScientificProfiles.AddAsync(profile, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
