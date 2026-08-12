using AssignmentSubmissionSystem.Api.Authentication;
using AssignmentSubmissionSystem.Application.Submissions;
using AssignmentSubmissionSystem.Domain.Submissions;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Student;

[Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Student)]
[ApiController]
[Route("api/student/assignments/{assignmentId:guid}/submission")]
public sealed class StudentSubmissionsController : ControllerBase
{
    private const long MaximumAttachmentBytes = 10 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> AllowedAttachmentContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".jpeg"] = "image/jpeg",
        [".jpg"] = "image/jpeg",
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".txt"] = "text/plain"
    };
    private readonly ISubmissionService _submissionService;

    public StudentSubmissionsController(ISubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    [HttpGet]
    public async Task<ActionResult<StudentSubmissionResponse>> GetAsync(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            Submission submission = await _submissionService.GetAsync(
                assignmentId,
                User.GetRequiredUserId(),
                cancellationToken);
            return Ok(StudentSubmissionResponse.From(submission));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public Task<ActionResult<StudentSubmissionResponse>> CreateAsync(Guid assignmentId, [FromForm] StudentSubmissionRequest request, CancellationToken cancellationToken)
    {
        return SaveAsync(() => _submissionService.CreateAsync(assignmentId, CreateCommand(request), User.GetRequiredUserId(), cancellationToken), true);
    }

    [HttpGet("/api/student/submissions/{submissionId:guid}/attachment")]
    public async Task<IActionResult> DownloadAttachmentAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        try
        {
            SubmissionAttachmentDownload attachment = await _submissionService.OpenAttachmentAsync(
                submissionId,
                User.GetRequiredUserId(),
                cancellationToken);
            return File(attachment.Content, attachment.ContentType, attachment.FileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut]
    public Task<ActionResult<StudentSubmissionResponse>> UpdateAsync(Guid assignmentId, [FromForm] StudentSubmissionRequest request, CancellationToken cancellationToken)
    {
        return SaveAsync(() => _submissionService.UpdateAsync(assignmentId, CreateCommand(request), User.GetRequiredUserId(), cancellationToken), false);
    }

    private static CreateSubmissionCommand CreateCommand(StudentSubmissionRequest request)
    {
        if (request.Attachment is null)
        {
            return new CreateSubmissionCommand { TextAnswer = request.TextAnswer };
        }

        string extension = Path.GetExtension(request.Attachment.FileName);
        if (request.Attachment.Length == 0 || request.Attachment.Length > MaximumAttachmentBytes || !AllowedAttachmentContentTypes.TryGetValue(extension, out string? contentType))
        {
            throw new ArgumentException("Attach one PDF, DOC, DOCX, TXT, PNG, JPG, or JPEG file no larger than 10 MB.");
        }

        return new CreateSubmissionCommand
        {
            Attachment = new SubmissionAttachmentUpload
            {
                Content = request.Attachment.OpenReadStream(),
                ContentType = contentType,
                OriginalFileName = request.Attachment.FileName
            },
            TextAnswer = request.TextAnswer
        };
    }

    private async Task<ActionResult<StudentSubmissionResponse>> SaveAsync(Func<Task<Submission>> save, bool created)
    {
        try
        {
            StudentSubmissionResponse response = StudentSubmissionResponse.From(await save());
            return created ? StatusCode(StatusCodes.Status201Created, response) : Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message, Status = StatusCodes.Status400BadRequest, Title = "Submission is not valid" });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Detail = exception.Message, Status = StatusCodes.Status409Conflict, Title = "Submission action is not allowed" });
        }
    }
}
