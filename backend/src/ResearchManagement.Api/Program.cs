using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Api.Infrastructure;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Infrastructure;
using ResearchManagement.Infrastructure.Auth;
using ResearchManagement.Infrastructure.Persistence.Seed;
using QuestPDF.Infrastructure;
using ResearchManagement.Api.BackgroundServices;
using ResearchManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Domain.Constants;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<WorkflowNotificationDispatcher>();

var jwt = JwtOptions.FromConfiguration(builder.Configuration);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (!context.Request.Headers.ContainsKey("Authorization"))
                    context.Token = context.Request.Cookies["research_session"];
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirst(AppClaimTypes.Subject)?.Value;
                if (!long.TryParse(subject, out var accountId))
                {
                    context.Fail("Token không xác định được tài khoản.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var account = await db.Accounts.AsNoTracking()
                    .Include(x => x.RoleAssignments)
                    .SingleOrDefaultAsync(x => x.Id == accountId, context.HttpContext.RequestAborted);
                if (account is null || account.Status != AccountStatuses.Active)
                {
                    context.Fail("Tài khoản không còn hoạt động.");
                    return;
                }

                var tokenVersion = context.Principal?.FindFirst(AppClaimTypes.AuthorizationVersion)?.Value;
                if (!string.Equals(tokenVersion, AuthorizationVersion.For(account), StringComparison.Ordinal))
                    context.Fail("Quyền hoặc thông tin xác thực đã thay đổi. Vui lòng đăng nhập lại.");
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = AppClaimTypes.Name,
            RoleClaimType = AppClaimTypes.Role
        };
    });
builder.Services.AddAuthorization(Policies.Register);

// Giới hạn đăng nhập: 10 lần/phút/IP để hạn chế dò mật khẩu.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});
builder.Services.AddHostedService<ReminderBackgroundService>();
var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("WebClient");
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") &&
        !context.Request.Path.StartsWithSegments("/api/integrations/copyleaks/webhooks") &&
        !HttpMethods.IsGet(context.Request.Method) &&
        !HttpMethods.IsHead(context.Request.Method) &&
        !HttpMethods.IsOptions(context.Request.Method) &&
        context.Request.Headers["X-Requested-With"] != "ResearchHub")
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { title = "Thiếu header X-Requested-With: ResearchHub." });
        return;
    }
    await next(context);
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await AdminSeeder.SeedAdminAsync(app.Services);
if (app.Environment.IsDevelopment())
{
    await DemoDataSeeder.SeedAsync(app.Services);
}

app.Run();

public partial class Program;
