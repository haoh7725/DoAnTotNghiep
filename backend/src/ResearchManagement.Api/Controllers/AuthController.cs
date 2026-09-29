using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Contracts;
using ResearchManagement.Api.Security;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;
namespace ResearchManagement.Api.Controllers;
[ApiController, Route("api/auth")]
public sealed class AuthController(ApplicationDbContext db, IPasswordHasher<Account> hasher,
    SessionTokens tokens, CurrentSession session) : ControllerBase
{
    // Constant, valid hash used to do equivalent password work for unknown usernames.
    private static readonly string DummyHash = new PasswordHasher<Account>().HashPassword(new Account(), Guid.NewGuid().ToString());
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.Username == request.Username.Trim(), ct);
        PasswordVerificationResult result;
        try { result = hasher.VerifyHashedPassword(account ?? new Account(), account?.PasswordHash ?? DummyHash, request.Password); }
        catch (FormatException) { result = PasswordVerificationResult.Failed; }
        if (account is null || result == PasswordVerificationResult.Failed || account.Status != "HOAT_DONG")
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa." });
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        { account.PasswordHash = hasher.HashPassword(account, request.Password); await db.SaveChangesAsync(ct); }
        var (token, expiry) = tokens.Create(account);
        Response.Cookies.Append(AuthSessionOptions.CookieName, token, new CookieOptions
        { HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Strict, Path = "/api", Expires = expiry, IsEssential = true });
        Response.Headers.CacheControl = "no-store";
        return Ok(new { accessToken = token, tokenType = "Bearer", expiresAt = expiry });
    }
    [HttpGet("me"), Authorize]
    public IActionResult Me() { Response.Headers.CacheControl = "no-store"; return Ok(session.Profile()); }
    [HttpPost("logout"), AllowAnonymous]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthSessionOptions.CookieName, new CookieOptions { Path = "/api", SameSite = SameSiteMode.Strict, Secure = Request.IsHttps });
        return NoContent();
    }
    [HttpPut("password"), Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var account = await db.Accounts.SingleAsync(x=>x.Id == session.Account.Id, ct);
        if (hasher.VerifyHashedPassword(account, account.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            return BadRequest(new ProblemDetails { Status = 400, Title = "Mật khẩu hiện tại không đúng." });
        account.PasswordHash = hasher.HashPassword(account, request.NewPassword); await db.SaveChangesAsync(ct);
        return Logout();
    }
}
