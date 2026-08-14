using AssignmentSubmissionSystem.Api.Authentication;
using AssignmentSubmissionSystem.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Notifications;

[Authorize(Policy = AuthorizationPolicies.NormalAccess)]
[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationItem>>> GetAsync(CancellationToken cancellationToken)
    {
        return Ok(await _notificationService.GetForCurrentUserAsync(User.GetRequiredUserId(), cancellationToken));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsReadAsync(Guid id, CancellationToken cancellationToken)
    {
        bool wasMarked = await _notificationService.MarkAsReadAsync(id, User.GetRequiredUserId(), cancellationToken);
        return wasMarked ? NoContent() : NotFound();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsReadAsync(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsReadAsync(User.GetRequiredUserId(), cancellationToken);
        return NoContent();
    }
}
