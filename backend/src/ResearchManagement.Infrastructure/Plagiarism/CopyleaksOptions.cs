using Microsoft.Extensions.Configuration;

namespace ResearchManagement.Infrastructure.Plagiarism;

public sealed record CopyleaksOptions(
    string Email,
    string ApiKey,
    string PublicBaseUrl,
    string WebhookSecret,
    bool Sandbox)
{
    public static CopyleaksOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Copyleaks");
        return new CopyleaksOptions(
            section["Email"] ?? string.Empty,
            section["ApiKey"] ?? string.Empty,
            (section["PublicBaseUrl"] ?? string.Empty).TrimEnd('/'),
            section["WebhookSecret"] ?? string.Empty,
            section.GetValue("Sandbox", true));
    }
}
