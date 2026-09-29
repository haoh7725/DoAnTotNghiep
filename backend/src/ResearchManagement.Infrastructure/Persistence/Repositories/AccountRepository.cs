using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Auth.Abstractions;
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

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken) =>
        db.Accounts.AnyAsync(a => a.Username == username, cancellationToken);

    public async Task AddAsync(Account account, CancellationToken cancellationToken) =>
        await db.Accounts.AddAsync(account, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
