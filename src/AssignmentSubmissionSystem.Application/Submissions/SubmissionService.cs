using AssignmentSubmissionSystem.Application.AcademicSetup.Enrollments;
using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Domain.Assignments;
using AssignmentSubmissionSystem.Domain.Submissions;

namespace AssignmentSubmissionSystem.Application.Submissions;

public sealed class SubmissionService : ISubmissionService
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IStudentEnrollmentRepository _studentEnrollmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ISubmissionFileStorage _submissionFileStorage;

    public SubmissionService(
        IAssignmentRepository assignmentRepository,
        IStudentEnrollmentRepository studentEnrollmentRepository,
        ISubmissionRepository submissionRepository,
        ISubmissionFileStorage submissionFileStorage)
    {
        _assignmentRepository = assignmentRepository;
        _studentEnrollmentRepository = studentEnrollmentRepository;
        _submissionRepository = submissionRepository;
        _submissionFileStorage = submissionFileStorage;
    }

    public async Task<Submission> CreateAsync(
        Guid assignmentId,
        CreateSubmissionCommand command,
        Guid studentUserId,
        CancellationToken cancellationToken)
    {
        Assignment assignment = await GetEligibleOpenAssignmentAsync(
            assignmentId,
            studentUserId,
            cancellationToken);
        Submission? existingSubmission = await _submissionRepository.GetByAssignmentAndStudentAsync(
            assignment.Id,
            studentUserId,
            cancellationToken);

        if (existingSubmission is not null)
        {
            throw new InvalidOperationException("You have already submitted work for this assignment.");
        }

        StoredSubmissionAttachment? attachment = command.Attachment is null
            ? null
            : await _submissionFileStorage.SaveAsync(command.Attachment, cancellationToken);
        Submission submission = new(
            Guid.CreateVersion7(),
            assignment.Id,
            studentUserId,
            command.TextAnswer,
            DateTimeOffset.UtcNow,
            attachment?.OriginalFileName,
            attachment?.ContentType,
            attachment?.StorageName);
        await _submissionRepository.AddAsync(submission, cancellationToken);

        return submission;
    }

    public async Task<StudentSubmissionDetails> GetAsync(
        Guid assignmentId,
        Guid studentUserId,
        CancellationToken cancellationToken)
    {
        Submission submission = await _submissionRepository.GetByAssignmentAndStudentAsync(
            assignmentId,
            studentUserId,
            cancellationToken)
            ?? throw new KeyNotFoundException("The requested submission was not found.");
        Assignment assignment = await _assignmentRepository.GetByIdAsync(assignmentId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested assignment was not found.");
        bool resultsAvailable = submission.Status == SubmissionStatus.Graded
            && assignment.Deadline <= DateTimeOffset.UtcNow;

        return new StudentSubmissionDetails
        {
            ResultsAvailable = resultsAvailable,
            Submission = submission
        };
    }

    public async Task<SubmissionAttachmentDownload> OpenAttachmentAsync(
        Guid submissionId,
        Guid studentUserId,
        CancellationToken cancellationToken)
    {
        Submission submission = await _submissionRepository.GetByIdAndStudentAsync(
            submissionId,
            studentUserId,
            cancellationToken)
            ?? throw new KeyNotFoundException("The requested submission was not found.");
        if (string.IsNullOrWhiteSpace(submission.AttachmentStorageName) ||
            string.IsNullOrWhiteSpace(submission.AttachmentContentType) ||
            string.IsNullOrWhiteSpace(submission.AttachmentFileName))
        {
            throw new KeyNotFoundException("The requested attachment was not found.");
        }

        Stream? content = await _submissionFileStorage.OpenReadAsync(
            submission.AttachmentStorageName,
            cancellationToken);
        if (content is null)
        {
            throw new KeyNotFoundException("The requested attachment was not found.");
        }

        return new SubmissionAttachmentDownload
        {
            Content = content,
            ContentType = submission.AttachmentContentType,
            FileName = submission.AttachmentFileName
        };
    }

    public async Task<IReadOnlyList<TeacherSubmissionItem>> GetForTeacherAssignmentAsync(
        Guid assignmentId,
        Guid teacherUserId,
        CancellationToken cancellationToken)
    {
        Assignment assignment = await _assignmentRepository.GetByIdAsync(assignmentId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested assignment was not found.");
        if (assignment.TeacherUserId != teacherUserId)
        {
            throw new KeyNotFoundException("The requested assignment was not found.");
        }

        return await _submissionRepository.GetForTeacherAssignmentAsync(
            assignmentId,
            teacherUserId,
            cancellationToken);
    }

    public Task<IReadOnlyList<TeacherSubmissionItem>> GetAllForAdminAsync(CancellationToken cancellationToken) => _submissionRepository.GetAllAsync(cancellationToken);

    public async Task<SubmissionAttachmentDownload> OpenAttachmentForAdminAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        Submission submission = await _submissionRepository.GetByIdAsync(submissionId, cancellationToken) ?? throw new KeyNotFoundException("The requested submission was not found.");
        return await OpenAttachmentAsync(submission, cancellationToken);
    }

    public async Task<SubmissionAttachmentDownload> OpenAttachmentForTeacherAsync(Guid submissionId, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Submission submission = await GetForTeacherAsync(submissionId, teacherUserId, cancellationToken);
        return await OpenAttachmentAsync(submission, cancellationToken);
    }

    public async Task GradeAsync(Guid submissionId, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Submission submission = await GetForTeacherAsync(submissionId, teacherUserId, cancellationToken);
        SubmissionReviewRevision revision = submission.CreateReviewRevision(Guid.CreateVersion7(), DateTimeOffset.UtcNow);
        submission.Grade();
        await _submissionRepository.UpdateWithReviewRevisionAsync(submission, revision, cancellationToken);
    }

    public async Task ReopenForCorrectionAsync(Guid submissionId, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Submission submission = await GetForTeacherAsync(submissionId, teacherUserId, cancellationToken);
        SubmissionReviewRevision revision = submission.CreateReviewRevision(Guid.CreateVersion7(), DateTimeOffset.UtcNow);
        submission.ReopenForCorrection();
        await _submissionRepository.UpdateWithReviewRevisionAsync(submission, revision, cancellationToken);
    }

    public async Task StartReviewAsync(Guid submissionId, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Submission submission = await GetForTeacherAsync(submissionId, teacherUserId, cancellationToken);
        SubmissionReviewRevision revision = submission.CreateReviewRevision(Guid.CreateVersion7(), DateTimeOffset.UtcNow);
        submission.StartReview();
        await _submissionRepository.UpdateWithReviewRevisionAsync(submission, revision, cancellationToken);
    }

    public async Task UpdateReviewAsync(
        Guid submissionId,
        ReviewSubmissionCommand command,
        Guid teacherUserId,
        CancellationToken cancellationToken)
    {
        Submission submission = await GetForTeacherAsync(submissionId, teacherUserId, cancellationToken);
        Assignment assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested submission was not found.");
        if (command.Marks is < 0 || command.Marks > assignment.MaximumMarks)
        {
            throw new ArgumentOutOfRangeException(nameof(command.Marks), "Marks must be between zero and the assignment maximum.");
        }

        SubmissionReviewRevision revision = submission.CreateReviewRevision(Guid.CreateVersion7(), DateTimeOffset.UtcNow);
        submission.UpdateReview(command.Marks, command.Feedback);
        await _submissionRepository.UpdateWithReviewRevisionAsync(submission, revision, cancellationToken);
    }

    public async Task<Submission> UpdateAsync(
        Guid assignmentId,
        CreateSubmissionCommand command,
        Guid studentUserId,
        CancellationToken cancellationToken)
    {
        Assignment assignment = await GetEligibleOpenAssignmentAsync(
            assignmentId,
            studentUserId,
            cancellationToken);

        if (assignment.AllowSubmissionUpdates != true)
        {
            throw new InvalidOperationException("This assignment does not allow submission updates.");
        }

        Submission submission = await _submissionRepository.GetByAssignmentAndStudentAsync(
            assignment.Id,
            studentUserId,
            cancellationToken)
            ?? throw new KeyNotFoundException("The requested submission was not found.");
        StoredSubmissionAttachment? attachment = command.Attachment is null
            ? null
            : await _submissionFileStorage.SaveAsync(command.Attachment, cancellationToken);
        DateTimeOffset updatedAt = DateTimeOffset.UtcNow;
        SubmissionRevision revision = submission.CreateRevision(Guid.CreateVersion7(), updatedAt);
        submission.UpdateContent(
            command.TextAnswer,
            attachment?.OriginalFileName,
            attachment?.ContentType,
            attachment?.StorageName,
            updatedAt);
        await _submissionRepository.UpdateWithRevisionAsync(submission, revision, cancellationToken);

        return submission;
    }

    private async Task<SubmissionAttachmentDownload> OpenAttachmentAsync(Submission submission, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(submission.AttachmentStorageName) || string.IsNullOrWhiteSpace(submission.AttachmentContentType) || string.IsNullOrWhiteSpace(submission.AttachmentFileName)) throw new KeyNotFoundException("The requested attachment was not found.");
        Stream? content = await _submissionFileStorage.OpenReadAsync(submission.AttachmentStorageName, cancellationToken);
        return content is null ? throw new KeyNotFoundException("The requested attachment was not found.") : new SubmissionAttachmentDownload { Content = content, ContentType = submission.AttachmentContentType, FileName = submission.AttachmentFileName };
    }

    private async Task<Submission> GetForTeacherAsync(
        Guid submissionId,
        Guid teacherUserId,
        CancellationToken cancellationToken)
    {
        return await _submissionRepository.GetByIdForTeacherAsync(
            submissionId,
            teacherUserId,
            cancellationToken)
            ?? throw new KeyNotFoundException("The requested submission was not found.");
    }

    private async Task<Assignment> GetEligibleOpenAssignmentAsync(
        Guid assignmentId,
        Guid studentUserId,
        CancellationToken cancellationToken)
    {
        Assignment assignment = await _assignmentRepository.GetByIdAsync(assignmentId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested assignment was not found.");

        bool hasActiveEnrollment = await _studentEnrollmentRepository.ExistsActiveAsync(
            studentUserId,
            assignment.ClassCourseId,
            cancellationToken);

        if (assignment.Status != AssignmentStatus.Published || !hasActiveEnrollment)
        {
            throw new KeyNotFoundException("The requested assignment was not found.");
        }

        if (assignment.Deadline is null || assignment.Deadline <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("The submission deadline has passed.");
        }

        return assignment;
    }
}
