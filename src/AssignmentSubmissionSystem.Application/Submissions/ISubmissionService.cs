using AssignmentSubmissionSystem.Domain.Submissions;

namespace AssignmentSubmissionSystem.Application.Submissions;

public interface ISubmissionService
{
    Task<Submission> CreateAsync(
        Guid assignmentId,
        CreateSubmissionCommand command,
        Guid studentUserId,
        CancellationToken cancellationToken);

    Task<StudentSubmissionDetails> GetAsync(
        Guid assignmentId,
        Guid studentUserId,
        CancellationToken cancellationToken);

    Task<SubmissionAttachmentDownload> OpenAttachmentAsync(
        Guid submissionId,
        Guid studentUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TeacherSubmissionItem>> GetForTeacherAssignmentAsync(
        Guid assignmentId,
        Guid teacherUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TeacherSubmissionItem>> GetAllForAdminAsync(CancellationToken cancellationToken);

    Task<SubmissionAttachmentDownload> OpenAttachmentForAdminAsync(Guid submissionId, CancellationToken cancellationToken);

    Task<SubmissionAttachmentDownload> OpenAttachmentForTeacherAsync(Guid submissionId, Guid teacherUserId, CancellationToken cancellationToken);

    Task GradeAsync(Guid submissionId, Guid teacherUserId, CancellationToken cancellationToken);

    Task ReopenForCorrectionAsync(Guid submissionId, Guid teacherUserId, CancellationToken cancellationToken);

    Task StartReviewAsync(Guid submissionId, Guid teacherUserId, CancellationToken cancellationToken);

    Task UpdateReviewAsync(
        Guid submissionId,
        ReviewSubmissionCommand command,
        Guid teacherUserId,
        CancellationToken cancellationToken);

    Task<Submission> UpdateAsync(
        Guid assignmentId,
        CreateSubmissionCommand command,
        Guid studentUserId,
        CancellationToken cancellationToken);
}
