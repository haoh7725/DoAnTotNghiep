using Microsoft.AspNetCore.Mvc;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult GetInfo() => Ok(new
    {
        application = "Research Management API",
        status = "running",
        utcTime = DateTimeOffset.UtcNow
    });
}
