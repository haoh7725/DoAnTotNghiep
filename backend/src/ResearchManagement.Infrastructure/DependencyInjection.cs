using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResearchManagement.Application.Auth;
using ResearchManagement.Application.Auth.Abstractions;
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
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(JwtOptions.FromConfiguration(configuration));
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordService, PasswordService>();

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<AuthService>();

        return services;
    }
}
