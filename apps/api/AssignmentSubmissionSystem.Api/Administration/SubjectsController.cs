using AssignmentSubmissionSystem.Application.AcademicSetup.Subjects;
using AssignmentSubmissionSystem.Domain.Academics;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Administration;

[Authorize(Roles = RoleNames.Admin)]
[ApiController]
[Route("api/admin/subjects")]
public sealed class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;

    public SubjectsController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    [HttpPost]
    public async Task<ActionResult<SubjectResponse>> CreateAsync(
        CreateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Subject subject = await _subjectService.CreateAsync(
                new CreateSubjectCommand
                {
                    Code = request.Code,
                    Name = request.Name
                },
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, ToResponse(subject));
        }
        catch (DuplicateSubjectCodeException exception)
        {
            return Conflict(new ProblemDetails
            {
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict,
                Title = "Subject code already exists"
            });
        }
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _subjectService.ArchiveAsync(id, cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SubjectResponse>> UpdateAsync(
        Guid id,
        CreateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Subject subject = await _subjectService.UpdateAsync(
                id,
                new CreateSubjectCommand
                {
                    Code = request.Code,
                    Name = request.Name
                },
                cancellationToken);

            return Ok(ToResponse(subject));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DuplicateSubjectCodeException exception)
        {
            return Conflict(new ProblemDetails
            {
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict,
                Title = "Subject code already exists"
            });
        }
    }

    private static SubjectResponse ToResponse(Subject subject)
    {
        return new SubjectResponse
        {
            Code = subject.Code,
            Id = subject.Id,
            IsArchived = subject.IsArchived,
            Name = subject.Name
        };
    }
}
