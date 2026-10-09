
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    // Accounts and organization
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Lecturer> Lecturers => Set<Lecturer>();

    // Research plans and progress
    public DbSet<ResearchPlan> ResearchPlans => Set<ResearchPlan>();
    public DbSet<ResearchPlanItem> ResearchPlanItems => Set<ResearchPlanItem>();
    public DbSet<ProgressHistory> ProgressHistories => Set<ProgressHistory>();

    // Notifications and reminders
    public DbSet<Notification> Notifications => Set<Notification>();

    // Research norms and targets
    public DbSet<ResearchNorm> ResearchNorms => Set<ResearchNorm>();
    public DbSet<ResearchTarget> ResearchTargets => Set<ResearchTarget>();
    public DbSet<ResearchTargetAllocation> ResearchTargetAllocations
        => Set<ResearchTargetAllocation>();

    // Lecturers and scientific profiles
    public DbSet<ScientificProfile> ScientificProfiles => Set<ScientificProfile>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();

    // Scientific products
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCoAuthor> ProductCoAuthors => Set<ProductCoAuthor>();
    public DbSet<ProductEvidence> ProductEvidences => Set<ProductEvidence>();
    public DbSet<ReviewHistory> ReviewHistories => Set<ReviewHistory>();

    // Conversion rules
    public DbSet<ConversionRule> ConversionRules => Set<ConversionRule>();
    public DbSet<ConversionCriterion> ConversionCriteria => Set<ConversionCriterion>();
    public DbSet<AuthorConversionCoefficient> AuthorConversionCoefficients
        => Set<AuthorConversionCoefficient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("nckh");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
