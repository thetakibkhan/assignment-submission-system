using AssignmentSubmissionSystem.Api.Authentication;
using AssignmentSubmissionSystem.Api.Teacher;
using AssignmentSubmissionSystem.Application.Submissions;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AssignmentSubmissionSystem.Api.Administration;
[Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Admin)]
[ApiController]
[Route("api/admin/submissions")]
public sealed class AdminSubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissionService;
    public AdminSubmissionsController(ISubmissionService submissionService) { _submissionService = submissionService; }
    [HttpGet] public async Task<ActionResult<IReadOnlyList<TeacherSubmissionResponse>>> GetAllAsync(CancellationToken cancellationToken) => Ok((await _submissionService.GetAllForAdminAsync(cancellationToken)).Select(TeacherSubmissionResponse.From).ToList());
    [HttpGet("{submissionId:guid}/attachment")] public async Task<IActionResult> DownloadAsync(Guid submissionId, CancellationToken cancellationToken) { try { SubmissionAttachmentDownload file = await _submissionService.OpenAttachmentForAdminAsync(submissionId, cancellationToken); return File(file.Content, file.ContentType, file.FileName); } catch (KeyNotFoundException) { return NotFound(); } }
}
