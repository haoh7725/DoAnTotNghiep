using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Auth;

/// <summary>Tên claim dùng trong JWT (dùng tên ngắn, không phụ thuộc ánh xạ mặc định của .NET).</summary>
public static class AppClaimTypes
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Role = "role";

    /// <summary>Mỗi phân quyền một claim, dạng "VAI_TRO|PHAM_VI|khoaId|boMonId" (id rỗng nếu không có).</summary>
    public const string Assignment = "phan_quyen";
}

public sealed class JwtTokenService(JwtOptions options, TimeProvider clock) : ITokenService
{
    public AccessToken CreateAccessToken(Account account)
    {
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(AppClaimTypes.Subject, account.Id.ToString()),
            new(AppClaimTypes.Name, account.Username)
        };

        foreach (var role in account.RoleAssignments.Select(r => r.Role).Distinct())
            claims.Add(new Claim(AppClaimTypes.Role, role));

        foreach (var r in account.RoleAssignments)
            claims.Add(new Claim(AppClaimTypes.Assignment, AssignmentClaim.Format(r.Role, r.Scope, r.FacultyId, r.DepartmentId)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AccessToken(token, expires);
    }
}

public static class AssignmentClaim
{
    public static string Format(string role, string scope, long? facultyId, long? departmentId) =>
        $"{role}|{scope}|{facultyId}|{departmentId}";

    public static RoleAssignmentDto? Parse(string value)
    {
        var parts = value.Split('|');
        if (parts.Length != 4) return null;
        return new RoleAssignmentDto(parts[0], parts[1], ToLong(parts[2]), ToLong(parts[3]));

        static long? ToLong(string s) => long.TryParse(s, out var v) ? v : null;
    }
}
