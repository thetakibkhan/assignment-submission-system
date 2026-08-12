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

    public async Task<Submission> GetAsync(
        Guid assignmentId,
        Guid studentUserId,
        CancellationToken cancellationToken)
    {
        return await _submissionRepository.GetByAssignmentAndStudentAsync(
            assignmentId,
            studentUserId,
            cancellationToken)
            ?? throw new KeyNotFoundException("The requested submission was not found.");
    }

    public async Task<SubmissionAttachmentDownload> OpenAttachmentAsync(
        Guid assignmentId,
        Guid studentUserId,
        CancellationToken cancellationToken)
    {
        Submission submission = await GetAsync(assignmentId, studentUserId, cancellationToken);
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
        submission.UpdateContent(
            command.TextAnswer,
            attachment?.OriginalFileName,
            attachment?.ContentType,
            attachment?.StorageName,
            DateTimeOffset.UtcNow);
        await _submissionRepository.UpdateAsync(submission, cancellationToken);

        return submission;
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
