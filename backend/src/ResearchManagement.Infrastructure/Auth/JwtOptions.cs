using Microsoft.Extensions.Configuration;

namespace ResearchManagement.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const int MinSecretLength = 32;

    public string Issuer { get; init; } = "ResearchManagement";
    public string Audience { get; init; } = "ResearchManagement.Clients";
    public string SecretKey { get; init; } = string.Empty;
    public int ExpiryMinutes { get; init; } = 60;

    public static JwtOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Jwt");
        var options = new JwtOptions
        {
            Issuer = section["Issuer"] ?? "ResearchManagement",
            Audience = section["Audience"] ?? "ResearchManagement.Clients",
            SecretKey = section["SecretKey"] ?? string.Empty,
            ExpiryMinutes = int.TryParse(section["ExpiryMinutes"], out var minutes) && minutes > 0 ? minutes : 60
        };

        if (options.SecretKey.Length < MinSecretLength)
            throw new InvalidOperationException(
                $"Jwt:SecretKey phải có ít nhất {MinSecretLength} ký tự (đặt qua biến môi trường Jwt__SecretKey).");

        return options;
    }
}
