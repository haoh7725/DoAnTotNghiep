using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Seed;

public static class AdminSeeder
{
    /// <summary>
    /// Tạo tài khoản QUAN_TRI đầu tiên từ cấu hình Seed:AdminUsername / Seed:AdminPassword.
    /// Bỏ qua nếu chưa cấu hình mật khẩu hoặc tài khoản đã tồn tại. Không bao giờ ghi đè.
    /// </summary>
    public static async Task SeedAdminAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");

        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Bỏ qua seed QUAN_TRI: chưa cấu hình Seed:AdminPassword.");
            return;
        }

        var username = configuration["Seed:AdminUsername"] ?? "admin";
        var fullName = configuration["Seed:AdminFullName"] ?? "Quản trị hệ thống";

        var accounts = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        const string testUsername = "p1_test";
        const string testPassword = "p1test123456";

        if (!await accounts.ExistsByUsernameAsync(testUsername, cancellationToken))
        {
            var testAccount = new Account(
                testUsername,
                string.Empty,
                "P1 Test User");

            testAccount.SetPasswordHash(
                passwords.Hash(testAccount, testPassword));

            await accounts.AddAsync(testAccount, cancellationToken);
            await accounts.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Đã tạo tài khoản test phân quyền {Username}.",
                testUsername);
        }

        if (await accounts.ExistsByUsernameAsync(username, cancellationToken))
        {
            logger.LogInformation("Tài khoản {Username} đã tồn tại, bỏ qua seed.", username);
            return;
        }

        var account = new Account(username, string.Empty, fullName);
        account.SetPasswordHash(passwords.Hash(account, password));
        account.AddRole(new RoleAssignment(Roles.Admin, Scopes.Global));

        await accounts.AddAsync(account, cancellationToken);
        await accounts.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Đã tạo tài khoản QUAN_TRI {Username}.", username);
    }
}
