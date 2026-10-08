using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResearchManagement.Application.Auth;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Infrastructure.Auth;
using ResearchManagement.Infrastructure.Persistence;
using ResearchManagement.Infrastructure.Persistence.Repositories;

using ResearchManagement.Application.ResearchPlans.Abstractions;
using ResearchManagement.Application.ResearchPlans;
using ResearchManagement.Application.ResearchNorms;
using ResearchManagement.Application.ResearchNorms.Abstractions;
using ResearchManagement.Application.ResearchTargets.Abstractions;
using ResearchManagement.Application.Lecturers;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Application.ProductTypes;
using ResearchManagement.Application.ProductTypes.Abstractions;
using ResearchManagement.Application.Products;
using ResearchManagement.Application.Products.Abstractions;

using ResearchManagement.Application.ResearchTargets;
using ResearchManagement.Application.ResearchTargetAllocations.Abstractions;
using ResearchManagement.Application.ResearchTargetAllocations;
namespace ResearchManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(JwtOptions.FromConfiguration(configuration));
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordService, PasswordService>();

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IResearchPlanRepository, ResearchPlanRepository>();
        services.AddScoped<IResearchPlanItemRepository, ResearchPlanItemRepository>();
        services.AddScoped<IProgressHistoryRepository, ProgressHistoryRepository>();
        services.AddScoped<AuthService>();
        services.AddScoped<ResearchPlanService>();

        services.AddScoped<ResearchPlanItemService>();
        services.AddScoped<IResearchNormRepository, ResearchNormRepository>();
        services.AddScoped<ResearchNormService>();
        services.AddScoped<IResearchNormRepository, ResearchNormRepository>();
        services.AddScoped<IResearchTargetRepository, ResearchTargetRepository>();

        services.AddScoped<ResearchTargetService>();
        services.AddScoped<IResearchTargetAllocationRepository, ResearchTargetAllocationRepository>();
        services.AddScoped<ResearchTargetAllocationService>();

        services.AddScoped<ILecturerRepository, LecturerRepository>();
        services.AddScoped<IScientificProfileRepository, ScientificProfileRepository>();
        services.AddScoped<IProductTypeRepository, ProductTypeRepository>();
        services.AddScoped<LecturerService>();
        services.AddScoped<ScientificProfileService>();
        services.AddScoped<ProductTypeService>();

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductCoAuthorRepository, ProductCoAuthorRepository>();
        services.AddScoped<IProductEvidenceRepository, ProductEvidenceRepository>();
        services.AddScoped<IReviewHistoryRepository, ReviewHistoryRepository>();
        services.AddScoped<ProductService>();
        services.AddScoped<CoAuthorService>();
        services.AddScoped<EvidenceService>();

        return services;
    }
}
