using Microsoft.EntityFrameworkCore;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();

    public DbSet<ResearchPlan> ResearchPlans => Set<ResearchPlan>();
    
    public DbSet<ResearchPlanItem> ResearchPlanItems => Set<ResearchPlanItem>();

    public DbSet<ProgressHistory> ProgressHistories => Set<ProgressHistory>();
    public DbSet<ResearchNorm> ResearchNorms => Set<ResearchNorm>();
    public DbSet<ResearchTarget> ResearchTargets => Set<ResearchTarget>();
    public DbSet<ResearchTargetAllocation> ResearchTargetAllocations
    => Set<ResearchTargetAllocation>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("nckh");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
