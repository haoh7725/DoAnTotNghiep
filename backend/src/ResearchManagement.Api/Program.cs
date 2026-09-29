using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ResearchManagement.Api.Middleware;
using ResearchManagement.Api.Security;
using ResearchManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSessionSecurity(builder.Configuration, builder.Environment);
builder.Services.AddExceptionHandler<ApiErrors>();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddPolicy("WebClient", policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseCors("WebClient");
// All mutating requests require a custom header. Cross-origin scripts need an approved CORS preflight.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") &&
        !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method) &&
        context.Request.Headers["X-Requested-With"] != "ResearchHub")
    { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { title = "Thiếu header X-Requested-With: ResearchHub." }); return; }
    await next(context);
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
if (args.Contains("--seed-demo"))
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Demo seeding is only allowed in Development.");
    await using var scope = app.Services.CreateAsyncScope();
    await DemoSeeder.SeedAsync(scope.ServiceProvider, builder.Configuration);
    return;
}
app.Run();
public partial class Program;
