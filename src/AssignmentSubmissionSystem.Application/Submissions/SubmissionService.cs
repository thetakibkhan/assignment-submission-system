using AssignmentSubmissionSystem.Application.AcademicSetup.Enrollments;
using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Domain.Assignments;
using AssignmentSubmissionSystem.Domain.Submissions;
using AssignmentSubmissionSystem.Domain.Notifications;

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

        Guid submissionId = Guid.CreateVersion7();
        IReadOnlyList<SubmissionAttachment> attachments = await SaveAttachmentsAsync(
            submissionId,
            command.Attachments,
            cancellationToken);
        Submission submission = new(
            submissionId,
            assignment.Id,
            studentUserId,
            command.TextAnswer,
            DateTimeOffset.UtcNow,
            attachments: attachments);
        UserNotification notification = new(
            Guid.CreateVersion7(),
            assignment.TeacherUserId,
            NotificationType.SubmissionReceived,
            assignment.Id,
            submission.Id,
            "A student submitted work for: " + assignment.Title,
            submission.SubmittedAt);
        await _submissionRepository.AddWithNotificationAsync(
            submission,
            notification,
            cancellationToken);

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
        Guid attachmentId,
        Guid studentUserId,
        CancellationToken cancellationToken)
    {
        Submission submission = await _submissionRepository.GetByIdAndStudentAsync(submissionId, studentUserId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested submission was not found.");
        return await OpenAttachmentAsync(submission, attachmentId, cancellationToken);
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
        return await OpenAttachmentAsync(submission, null, cancellationToken);
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

    public async Task<SubmissionAttachmentDownload> OpenAttachmentForAdminAsync(Guid submissionId, Guid attachmentId, CancellationToken cancellationToken)
    {
        Submission submission = await _submissionRepository.GetByIdAsync(submissionId, cancellationToken) ?? throw new KeyNotFoundException("The requested submission was not found.");
        return await OpenAttachmentAsync(submission, attachmentId, cancellationToken);
    }

    public async Task<SubmissionAttachmentDownload> OpenAttachmentForAdminAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        Submission submission = await _submissionRepository.GetByIdAsync(submissionId, cancellationToken) ?? throw new KeyNotFoundException("The requested submission was not found.");
        return await OpenAttachmentAsync(submission, null, cancellationToken);
    }

    public async Task<SubmissionAttachmentDownload> OpenAttachmentForTeacherAsync(Guid submissionId, Guid attachmentId, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Submission submission = await GetForTeacherAsync(submissionId, teacherUserId, cancellationToken);
        return await OpenAttachmentAsync(submission, attachmentId, cancellationToken);
    }

    public async Task<SubmissionAttachmentDownload> OpenAttachmentForTeacherAsync(Guid submissionId, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Submission submission = await GetForTeacherAsync(submissionId, teacherUserId, cancellationToken);
        return await OpenAttachmentAsync(submission, null, cancellationToken);
    }

    public async Task GradeAsync(Guid submissionId, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Submission submission = await GetForTeacherAsync(submissionId, teacherUserId, cancellationToken);
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        SubmissionReviewRevision revision = submission.CreateReviewRevision(Guid.CreateVersion7(), createdAt);
        submission.Grade();
        UserNotification notification = new(Guid.CreateVersion7(), submission.StudentUserId, NotificationType.SubmissionGraded, submission.AssignmentId, submission.Id, "Your submission has been graded. Results are visible after the deadline.", createdAt);
        await _submissionRepository.UpdateWithReviewRevisionAndNotificationAsync(submission, revision, notification, cancellationToken);
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
        List<SubmissionAttachment> removedAttachments = submission.Attachments
            .Where(attachment => command.RemovedAttachmentIds.Contains(attachment.Id))
            .ToList();
        int retainedAttachmentCount = submission.Attachments.Count - removedAttachments.Count;
        if (retainedAttachmentCount + command.Attachments.Count > 5)
        {
            throw new ArgumentException("A submission can contain at most 5 attachments.");
        }

        IReadOnlyList<SubmissionAttachment> newAttachments = await SaveAttachmentsAsync(submission.Id, command.Attachments, cancellationToken);
        DateTimeOffset updatedAt = DateTimeOffset.UtcNow;
        SubmissionRevision revision = submission.CreateRevision(Guid.CreateVersion7(), updatedAt);
        submission.UpdateContent(command.TextAnswer, command.RemovedAttachmentIds, newAttachments, updatedAt);
        await _submissionRepository.UpdateWithRevisionAsync(submission, revision, newAttachments, removedAttachments, cancellationToken);
        foreach (SubmissionAttachment removedAttachment in removedAttachments)
        {
            await _submissionFileStorage.DeleteAsync(removedAttachment.StorageName, cancellationToken);
        }

        return submission;
    }

    private async Task<IReadOnlyList<SubmissionAttachment>> SaveAttachmentsAsync(
        Guid submissionId,
        IReadOnlyList<SubmissionAttachmentUpload> uploads,
        CancellationToken cancellationToken)
    {
        List<SubmissionAttachment> attachments = [];
        foreach (SubmissionAttachmentUpload upload in uploads)
        {
            StoredSubmissionAttachment stored = await _submissionFileStorage.SaveAsync(upload, cancellationToken);
            attachments.Add(new SubmissionAttachment(
                Guid.CreateVersion7(),
                submissionId,
                stored.OriginalFileName,
                stored.ContentType,
                stored.StorageName));
        }

        return attachments;
    }

    private async Task<SubmissionAttachmentDownload> OpenAttachmentAsync(
        Submission submission,
        Guid? attachmentId,
        CancellationToken cancellationToken)
    {
        SubmissionAttachment? attachment = attachmentId.HasValue
            ? submission.Attachments.SingleOrDefault(item => item.Id == attachmentId.Value)
            : submission.Attachments.FirstOrDefault();
        if (attachment is not null)
        {
            Stream? content = await _submissionFileStorage.OpenReadAsync(attachment.StorageName, cancellationToken);
            return content is null
                ? throw new KeyNotFoundException("The requested attachment was not found.")
                : new SubmissionAttachmentDownload { Content = content, ContentType = attachment.ContentType, FileName = attachment.FileName };
        }

        if (string.IsNullOrWhiteSpace(submission.AttachmentStorageName) || string.IsNullOrWhiteSpace(submission.AttachmentContentType) || string.IsNullOrWhiteSpace(submission.AttachmentFileName))
        {
            throw new KeyNotFoundException("The requested attachment was not found.");
        }

        Stream? legacyContent = await _submissionFileStorage.OpenReadAsync(submission.AttachmentStorageName, cancellationToken);
        return legacyContent is null
            ? throw new KeyNotFoundException("The requested attachment was not found.")
            : new SubmissionAttachmentDownload { Content = legacyContent, ContentType = submission.AttachmentContentType, FileName = submission.AttachmentFileName };
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
