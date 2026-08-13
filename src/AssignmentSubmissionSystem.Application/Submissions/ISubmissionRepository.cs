using AssignmentSubmissionSystem.Domain.Submissions;

namespace AssignmentSubmissionSystem.Application.Submissions;

public interface ISubmissionRepository
{
    Task AddAsync(Submission submission, CancellationToken cancellationToken);

    Task<IReadOnlyList<TeacherSubmissionItem>> GetForTeacherAssignmentAsync(
        Guid assignmentId,
        Guid teacherUserId,
        CancellationToken cancellationToken);

    Task<Submission?> GetByIdForTeacherAsync(
        Guid submissionId,
        Guid teacherUserId,
        CancellationToken cancellationToken);

    Task<Submission?> GetByIdAndStudentAsync(
        Guid submissionId,
        Guid studentUserId,
        CancellationToken cancellationToken);

    Task<Submission?> GetByAssignmentAndStudentAsync(
        Guid assignmentId,
        Guid studentUserId,
        CancellationToken cancellationToken);

    Task UpdateWithReviewRevisionAsync(
        Submission submission,
        SubmissionReviewRevision revision,
        CancellationToken cancellationToken);

    Task UpdateWithRevisionAsync(
        Submission submission,
        SubmissionRevision revision,
        CancellationToken cancellationToken);
}
