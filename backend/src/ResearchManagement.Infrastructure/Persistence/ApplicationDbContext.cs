using Microsoft.EntityFrameworkCore;
using ResearchManagement.Domain.Entities;
namespace ResearchManagement.Infrastructure.Persistence;
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("nckh");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
