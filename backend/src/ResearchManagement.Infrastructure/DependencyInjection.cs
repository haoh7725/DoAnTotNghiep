
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ResearchManagement.Application.Auth;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.ResearchPlans;
using ResearchManagement.Application.ResearchPlans.Abstractions;
using ResearchManagement.Application.Notifications;
using ResearchManagement.Application.Notifications.Abstractions;

using ResearchManagement.Application.ResearchNorms;
using ResearchManagement.Application.ResearchNorms.Abstractions;
using ResearchManagement.Application.ResearchTargets;
using ResearchManagement.Application.ResearchTargets.Abstractions;
using ResearchManagement.Application.ResearchTargetAllocations;
using ResearchManagement.Application.ResearchTargetAllocations.Abstractions;

using ResearchManagement.Application.Lecturers;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Application.ProductTypes;
using ResearchManagement.Application.ProductTypes.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Products;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Infrastructure.Storage;

using ResearchManagement.Infrastructure.Auth;
using ResearchManagement.Infrastructure.Persistence;
using ResearchManagement.Infrastructure.Persistence.Repositories;

namespace ResearchManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(JwtOptions.FromConfiguration(configuration));
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordService, PasswordService>();

        // Authentication
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<AuthService>();

        // Research plans and progress
        services.AddScoped<IResearchPlanRepository, ResearchPlanRepository>();
        services.AddScoped<IResearchPlanItemRepository, ResearchPlanItemRepository>();
        services.AddScoped<IProgressHistoryRepository, ProgressHistoryRepository>();
        services.AddScoped<ILecturerAccessRepository, LecturerAccessRepository>();
        services.AddScoped<ResearchPlanService>();
        services.AddScoped<ResearchPlanItemService>();

        // Notifications and deadline reminders
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();
        services.AddScoped<NotificationService>();
        services.AddScoped<ReminderService>();

        // Research norms
        services.AddScoped<IResearchNormRepository, ResearchNormRepository>();
        services.AddScoped<ResearchNormService>();

        // Research targets and allocations
        services.AddScoped<IResearchTargetRepository, ResearchTargetRepository>();
        services.AddScoped<ResearchTargetService>();
        services.AddScoped<IResearchTargetAllocationRepository, ResearchTargetAllocationRepository>();
        services.AddScoped<ResearchTargetAllocationService>();

        // Lecturers and scientific profiles
        services.AddScoped<ILecturerRepository, LecturerRepository>();
        services.AddScoped<IScientificProfileRepository, ScientificProfileRepository>();
        services.AddScoped<LecturerService>();
        services.AddScoped<ScientificProfileService>();

        // Product types
        services.AddScoped<IProductTypeRepository, ProductTypeRepository>();
        services.AddScoped<ProductTypeService>();

        // Scientific products
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductEvidenceRepository, ProductEvidenceRepository>();
        services.AddScoped<IReviewHistoryRepository, ReviewHistoryRepository>();
        services.AddScoped<ProductService>();
        services.AddScoped<ProductAuthorService>();
        services.AddScoped<EvidenceService>();

        return services;
    }
}
