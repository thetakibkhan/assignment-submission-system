using AssignmentSubmissionSystem.Application.AcademicSetup.TeacherResponsibilities;
using AssignmentSubmissionSystem.Domain.Assignments;

namespace AssignmentSubmissionSystem.Application.Assignments;

public sealed class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ITeacherResponsibilityRepository _teacherResponsibilityRepository;

    public AssignmentService(IAssignmentRepository assignmentRepository, ITeacherResponsibilityRepository teacherResponsibilityRepository)
    {
        _assignmentRepository = assignmentRepository;
        _teacherResponsibilityRepository = teacherResponsibilityRepository;
    }

    public async Task<Assignment> CreateAsync(CreateAssignmentCommand command, Guid teacherUserId, CancellationToken cancellationToken)
    {
        await EnsureActiveScopeAsync(command.ClassCourseId, command.SubjectId, teacherUserId, cancellationToken);
        Assignment assignment = new(Guid.CreateVersion7(), teacherUserId, command.ClassCourseId, command.SubjectId, command.Title, command.Description, command.Deadline, command.MaximumMarks, command.AllowSubmissionUpdates, DateTimeOffset.UtcNow);
        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        return assignment;
    }

    public Task<IReadOnlyList<Assignment>> GetForTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken)
        => _assignmentRepository.GetForTeacherAsync(teacherUserId, cancellationToken);

    public Task<IReadOnlyList<TeacherAssignmentScope>> GetScopesForTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken)
        => _teacherResponsibilityRepository.GetActiveScopesForTeacherAsync(teacherUserId, cancellationToken);

    public async Task<Assignment> UpdateAsync(Guid id, CreateAssignmentCommand command, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Assignment assignment = await GetOwnedAsync(id, teacherUserId, cancellationToken);
        await EnsureActiveScopeAsync(command.ClassCourseId, command.SubjectId, teacherUserId, cancellationToken);
        assignment.Update(command.ClassCourseId, command.SubjectId, command.Title, command.Description, command.Deadline, command.MaximumMarks, command.AllowSubmissionUpdates, await _assignmentRepository.HasSubmissionsAsync(id, cancellationToken), DateTimeOffset.UtcNow);
        await _assignmentRepository.UpdateAsync(assignment, cancellationToken);
        return assignment;
    }

    public async Task PublishAsync(Guid id, Guid teacherUserId, CancellationToken cancellationToken)
    {
        Assignment assignment = await GetOwnedAsync(id, teacherUserId, cancellationToken);
        await EnsureActiveScopeAsync(assignment.ClassCourseId, assignment.SubjectId, teacherUserId, cancellationToken);
        assignment.Publish(DateTimeOffset.UtcNow);
        await _assignmentRepository.UpdateAsync(assignment, cancellationToken);
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

    private async Task EnsureActiveScopeAsync(Guid classCourseId, Guid subjectId, Guid teacherUserId, CancellationToken cancellationToken)
    {
        if (!await _teacherResponsibilityRepository.ExistsActiveForTeacherAsync(classCourseId, subjectId, teacherUserId, cancellationToken))
        {
            throw new UnauthorizedAccessException("You are not assigned to the selected Class/Course and Subject.");
        }
    }
}
