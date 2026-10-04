using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ResearchManagement.Api.Contracts;
using ResearchManagement.Application.Auth;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Application.Common;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AuthService auth,
    ICurrentUser currentUser,
    IAccountRepository accounts,
    IPasswordService passwords) : ControllerBase
{
    private const string SessionCookie = "research_session";

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request, cancellationToken);
        Response.Cookies.Append(SessionCookie, result.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api",
            Expires = result.ExpiresAt,
            IsEssential = true
        });
        Response.Headers.CacheControl = "no-store";
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ProfileResponse>> Me(CancellationToken cancellationToken)
    {
        var id = currentUser.Id ?? throw new AuthenticationFailedException("Token không hợp lệ.");
        Response.Headers.CacheControl = "no-store";
        return Ok(await auth.GetProfileAsync(id, cancellationToken));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(SessionCookie, new CookieOptions
        {
            Path = "/api",
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict
        });
        return NoContent();
    }

    [HttpPut("password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var id = currentUser.Id ?? throw new AuthenticationFailedException("Token không hợp lệ.");
        var account = await accounts.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");
        if (!passwords.Verify(account, request.CurrentPassword).Succeeded)
            throw new BusinessRuleException("Mật khẩu hiện tại không đúng.");

        account.SetPasswordHash(passwords.Hash(account, request.NewPassword));
        await accounts.SaveChangesAsync(cancellationToken);
        return Logout();
    }
}
