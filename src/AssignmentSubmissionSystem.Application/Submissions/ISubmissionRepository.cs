using AssignmentSubmissionSystem.Domain.Submissions;
using AssignmentSubmissionSystem.Domain.Notifications;

namespace AssignmentSubmissionSystem.Application.Submissions;

public interface ISubmissionRepository
{
    Task AddWithNotificationAsync(
        Submission submission,
        UserNotification notification,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TeacherSubmissionItem>> GetForTeacherAssignmentAsync(
        Guid assignmentId,
        Guid teacherUserId,
        CancellationToken cancellationToken);

    Task<Submission?> GetByIdAsync(Guid submissionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TeacherSubmissionItem>> GetAllAsync(CancellationToken cancellationToken);

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

    Task UpdateWithReviewRevisionAndNotificationAsync(
        Submission submission,
        SubmissionReviewRevision revision,
        UserNotification notification,
        CancellationToken cancellationToken);
}
