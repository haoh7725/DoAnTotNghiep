
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.Notifications;
using ResearchManagement.Application.Notifications.Models;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(
    NotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await service.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadNotificationCountResponse>> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        var result = await service.GetUnreadCountAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:long}/read")]
    public async Task<ActionResult<NotificationResponse>> MarkAsRead(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await service.MarkAsReadAsync(
            id,
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(
        CancellationToken cancellationToken)
    {
        var updatedCount =
            await service.MarkAllAsReadAsync(cancellationToken);

        return Ok(new { updatedCount });
    }
}
