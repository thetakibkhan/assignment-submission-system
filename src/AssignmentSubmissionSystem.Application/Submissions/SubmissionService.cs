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

    public SubmissionService(
        IAssignmentRepository assignmentRepository,
        IStudentEnrollmentRepository studentEnrollmentRepository,
        ISubmissionRepository submissionRepository)
    {
        _assignmentRepository = assignmentRepository;
        _studentEnrollmentRepository = studentEnrollmentRepository;
        _submissionRepository = submissionRepository;
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

        Submission submission = new(
            Guid.CreateVersion7(),
            assignment.Id,
            studentUserId,
            command.TextAnswer,
            DateTimeOffset.UtcNow);
        await _submissionRepository.AddAsync(submission, cancellationToken);

        return submission;
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
        submission.UpdateTextAnswer(command.TextAnswer, DateTimeOffset.UtcNow);
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
