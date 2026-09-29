using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResearchManagement.Application.Security;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;
namespace ResearchManagement.Api.Security;

public sealed class AuthSessionOptions
{
    public const string CookieName = "research_session";
    public string Issuer { get; set; } = "ResearchManagement";
    public string Audience { get; set; } = "ResearchManagement.Clients";
    public string SigningKey { get; set; } = "";
    public int LifetimeMinutes { get; set; } = 30;
}
public sealed class SessionTokens(AuthSessionOptions options, TimeProvider time)
{
    public static string PasswordStamp(string hash) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
    public (string Token, DateTimeOffset ExpiresAt) Create(Account account)
    {
        var now = time.GetUtcNow(); var expiry = now.AddMinutes(options.LifetimeMinutes);
        var token = new JwtSecurityToken(options.Issuer, options.Audience,
            [new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
             new Claim("password_stamp", PasswordStamp(account.PasswordHash)),
             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())],
            now.UtcDateTime, expiry.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)), SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }
}
// Loaded from the database on every authenticated request, never trusted from a client.
public sealed class CurrentSession
{
    public Account Account { get; set; } = null!;
    public List<Permission> Permissions { get; set; } = [];
    public bool IsAdministrator => Permissions.Any(p => p.Role == "QUAN_TRI" && PermissionRules.IsValid(p));
    public bool CanRead(long ownerId, long facultyId, long departmentId) =>
        PermissionRules.CanRead(Permissions, Account.Id, ownerId, facultyId, departmentId);
    public object Profile() => new { Account.Id, Account.Username, Account.FullName, Account.Email, permissions = Permissions };
}
public static class SessionSecurity
{
    public static void AddSessionSecurity(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        var options = config.GetSection("Auth").Get<AuthSessionOptions>() ?? new();
        if (string.IsNullOrWhiteSpace(options.SigningKey) && env.IsDevelopment())
            options.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        if (Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
            throw new InvalidOperationException("Set Auth__SigningKey to a random secret of at least 32 bytes.");
        if (options.LifetimeMinutes is < 5 or > 120) throw new InvalidOperationException("Auth lifetime must be 5–120 minutes.");
        services.AddSingleton(options); services.AddSingleton(TimeProvider.System);
        services.AddSingleton<SessionTokens>(); services.AddScoped<CurrentSession>();
        services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
        {
            o.MapInboundClaims = false;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = options.Issuer,
                ValidateAudience = true, ValidAudience = options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(15),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256], NameClaimType = "sub", RoleClaimType = ClaimTypes.Role
            };
            o.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (!context.Request.Headers.ContainsKey("Authorization"))
                        context.Token = context.Request.Cookies[AuthSessionOptions.CookieName];
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    if (!long.TryParse(context.Principal?.FindFirstValue("sub"), out var id)) { context.Fail("Invalid subject"); return; }
                    var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, context.HttpContext.RequestAborted);
                    if (account is null || account.Status != "HOAT_DONG" ||
                        context.Principal?.FindFirstValue("password_stamp") != SessionTokens.PasswordStamp(account.PasswordHash))
                    { context.Fail("Session is no longer valid"); return; }
                    var current = context.HttpContext.RequestServices.GetRequiredService<CurrentSession>();
                    current.Account = account;
                    current.Permissions = await db.RoleAssignments.AsNoTracking().Where(x => x.AccountId == id)
                        .Select(x => new Permission(x.Role, x.Scope, x.FacultyId, x.DepartmentId)).ToListAsync(context.HttpContext.RequestAborted);
                    var identity = (ClaimsIdentity)context.Principal!.Identity!;
                    foreach (var permission in current.Permissions.Where(PermissionRules.IsValid))
                        identity.AddClaim(new Claim(ClaimTypes.Role, permission.Role));
                }
            };
        });
        services.AddAuthorization(o => o.AddPolicy("Administrator", p => p.RequireAuthenticatedUser().RequireRole("QUAN_TRI")));
    }
}
