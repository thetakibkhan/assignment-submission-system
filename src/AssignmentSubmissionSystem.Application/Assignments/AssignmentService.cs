using AssignmentSubmissionSystem.Application.AcademicSetup.TeacherResponsibilities;
using AssignmentSubmissionSystem.Application.AcademicSetup.Enrollments;
using AssignmentSubmissionSystem.Domain.Notifications;
using AssignmentSubmissionSystem.Domain.Assignments;

namespace AssignmentSubmissionSystem.Application.Assignments;

public sealed class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ITeacherResponsibilityRepository _teacherResponsibilityRepository;
    private readonly IStudentEnrollmentRepository _studentEnrollmentRepository;

    public AssignmentService(IAssignmentRepository assignmentRepository, ITeacherResponsibilityRepository teacherResponsibilityRepository, IStudentEnrollmentRepository studentEnrollmentRepository)
    {
        _assignmentRepository = assignmentRepository;
        _teacherResponsibilityRepository = teacherResponsibilityRepository;
        _studentEnrollmentRepository = studentEnrollmentRepository;
    }

    public async Task<Assignment> CreateAsync(CreateAssignmentCommand command, Guid teacherUserId, CancellationToken cancellationToken)
    {
        await EnsureActiveScopeAsync(command.AcademicClassId, command.SubjectId, teacherUserId, cancellationToken);
        Assignment assignment = new(Guid.CreateVersion7(), teacherUserId, command.AcademicClassId, command.SubjectId, command.Title, command.Description, command.Deadline, command.MaximumMarks, command.AllowSubmissionUpdates, DateTimeOffset.UtcNow);
        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        return assignment;
    }

    public Task<IReadOnlyList<Assignment>> GetForTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken)
        => _assignmentRepository.GetForTeacherAsync(teacherUserId, cancellationToken);

    public Task<IReadOnlySet<Guid>> GetIdsWithSubmissionsAsync(
        IReadOnlyCollection<Guid> assignmentIds,
        CancellationToken cancellationToken)
        => _assignmentRepository.GetIdsWithSubmissionsAsync(assignmentIds, cancellationToken);

    public Task<IReadOnlyList<TeacherAssignmentScope>> GetScopesForTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken)
        => _teacherResponsibilityRepository.GetActiveScopesForTeacherAsync(teacherUserId, cancellationToken);

    public async Task<Assignment> UpdateAsync(Guid id, CreateAssignmentCommand command, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Assignment assignment = await GetOwnedAsync(id, teacherUserId, cancellationToken);
        await EnsureActiveScopeAsync(command.AcademicClassId, command.SubjectId, teacherUserId, cancellationToken);
        assignment.Update(command.AcademicClassId, command.SubjectId, command.Title, command.Description, command.Deadline, command.MaximumMarks, command.AllowSubmissionUpdates, await _assignmentRepository.HasSubmissionsAsync(id, cancellationToken), DateTimeOffset.UtcNow);
        await _assignmentRepository.UpdateAsync(assignment, cancellationToken);
        return assignment;
    }

    public async Task PublishAsync(Guid id, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Assignment assignment = await GetOwnedAsync(id, teacherUserId, cancellationToken);
        await EnsureActiveScopeAsync(assignment.AcademicClassId, assignment.SubjectId, teacherUserId, cancellationToken);
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        assignment.Publish(createdAt);
        IReadOnlyList<Guid> studentUserIds = await _studentEnrollmentRepository.GetActiveStudentUserIdsAsync(assignment.AcademicClassId, cancellationToken);
        IReadOnlyList<UserNotification> notifications = studentUserIds
            .Select(studentUserId => new UserNotification(Guid.CreateVersion7(), studentUserId, NotificationType.AssignmentPublished, assignment.Id, null, "A new assignment is available: " + assignment.Title, createdAt))
            .ToList();
        await _assignmentRepository.UpdateWithNotificationsAsync(assignment, notifications, cancellationToken);
    }

    public async Task UnpublishAsync(Guid id, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Assignment assignment = await GetOwnedAsync(id, teacherUserId, cancellationToken);
        assignment.Unpublish(await _assignmentRepository.HasSubmissionsAsync(id, cancellationToken));
        await _assignmentRepository.UpdateAsync(assignment, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Assignment assignment = await GetOwnedAsync(id, teacherUserId, cancellationToken);
        if (await _assignmentRepository.HasSubmissionsAsync(id, cancellationToken))
        {
            throw new InvalidOperationException("An assignment with submissions cannot be deleted.");
        }
        await _assignmentRepository.DeleteAsync(assignment, cancellationToken);
    }

    private async Task<Assignment> GetOwnedAsync(Guid id, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Assignment assignment = await _assignmentRepository.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("The requested assignment was not found.");
        if (assignment.TeacherUserId != teacherUserId)
        {
            throw new UnauthorizedAccessException("You can manage only your own assignments.");
        }
        return assignment;
    }

    private async Task EnsureActiveScopeAsync(Guid academicClassId, Guid subjectId, Guid teacherUserId, CancellationToken cancellationToken)
    {
        if (!await _teacherResponsibilityRepository.ExistsActiveForTeacherAsync(academicClassId, subjectId, teacherUserId, cancellationToken))
        {
            throw new UnauthorizedAccessException("You are not assigned to the selected Class and Subject.");
        }
    }
}
