using AssignmentSubmissionSystem.Domain.Assignments;

namespace AssignmentSubmissionSystem.Application.Assignments;

public interface IAssignmentService
{
    Task<Assignment> CreateAsync(CreateAssignmentCommand command, Guid teacherUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Assignment>> GetForTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetIdsWithSubmissionsAsync(IReadOnlyCollection<Guid> assignmentIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<TeacherAssignmentScope>> GetScopesForTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken);
    Task<Assignment> UpdateAsync(Guid id, CreateAssignmentCommand command, Guid teacherUserId, CancellationToken cancellationToken);
    Task PublishAsync(Guid id, Guid teacherUserId, CancellationToken cancellationToken);
    Task UnpublishAsync(Guid id, Guid teacherUserId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, Guid teacherUserId, CancellationToken cancellationToken);
}
