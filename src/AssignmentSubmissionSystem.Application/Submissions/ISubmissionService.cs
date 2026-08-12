using AssignmentSubmissionSystem.Domain.Submissions;

namespace AssignmentSubmissionSystem.Application.Submissions;

public interface ISubmissionService
{
    Task<Submission> CreateAsync(
        Guid assignmentId,
        CreateSubmissionCommand command,
        Guid studentUserId,
        CancellationToken cancellationToken);

    Task<Submission> GetAsync(
        Guid assignmentId,
        Guid studentUserId,
        CancellationToken cancellationToken);

    Task<Submission> UpdateAsync(
        Guid assignmentId,
        CreateSubmissionCommand command,
        Guid studentUserId,
        CancellationToken cancellationToken);
}
