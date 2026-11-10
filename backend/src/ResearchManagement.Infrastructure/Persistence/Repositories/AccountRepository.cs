using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository(ApplicationDbContext db) : IAccountRepository
{
    public Task<Account?> GetByUsernameAsync(string username, CancellationToken cancellationToken) =>
        db.Accounts.Include(a => a.RoleAssignments)
            .FirstOrDefaultAsync(a => a.Username == username, cancellationToken);

    public Task<Account?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        db.Accounts.Include(a => a.RoleAssignments)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LecturerAccountOptionResponse>> GetAvailableLecturerAccountsAsync(
        CancellationToken cancellationToken)
    {
        var eligibleRoles = new[] { Roles.Lecturer, Roles.DepartmentHead, Roles.FacultyDean };
        var officeRoles = new[] { Roles.ResearchOffice, Roles.BoardOfDirectors, Roles.Admin };
        var rows = await db.Accounts.AsNoTracking()
            .Where(account => account.Status == AccountStatuses.Active)
            .Where(account => !db.Lecturers.Any(lecturer => lecturer.AccountId == account.Id))
            .Where(account => account.RoleAssignments.Any(role => eligibleRoles.Contains(role.Role)))
            .Where(account => !account.RoleAssignments.Any(role => officeRoles.Contains(role.Role)))
            .OrderBy(account => account.FullName).ThenBy(account => account.Username)
            .Select(account => new
            {
                account.Id,
                account.Username,
                account.FullName,
                account.Email,
                Roles = account.RoleAssignments
                    .Where(role => eligibleRoles.Contains(role.Role))
                    .Select(role => role.Role)
                    .Distinct()
                    .OrderBy(role => role)
                    .ToList(),
                FacultyId = account.RoleAssignments
                    .Where(role => eligibleRoles.Contains(role.Role) && role.FacultyId != null)
                    .Select(role => role.FacultyId)
                    .FirstOrDefault(),
                DepartmentId = account.RoleAssignments
                    .Where(role => eligibleRoles.Contains(role.Role) && role.DepartmentId != null)
                    .Select(role => role.DepartmentId)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return rows.Select(account => new LecturerAccountOptionResponse(
            account.Id, account.Username, account.FullName, account.Email, account.Roles,
            account.FacultyId, account.DepartmentId)).ToList();
    }

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken) =>
        db.Accounts.AnyAsync(a => a.Username == username, cancellationToken);

    public async Task AddAsync(Account account, CancellationToken cancellationToken) =>
        await db.Accounts.AddAsync(account, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
