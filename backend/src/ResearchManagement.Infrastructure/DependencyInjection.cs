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

        return services;
    }
}
