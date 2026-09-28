using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ResearchManagement.Application.Auth;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Application.Common;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.LoginAsync(request, cancellationToken));

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ProfileResponse>> Me(CancellationToken cancellationToken)
    {
        var id = currentUser.Id ?? throw new AuthenticationFailedException("Token không hợp lệ.");
        return Ok(await auth.GetProfileAsync(id, cancellationToken));
    }
}
