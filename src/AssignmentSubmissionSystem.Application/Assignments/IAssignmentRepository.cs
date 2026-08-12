using AssignmentSubmissionSystem.Domain.Assignments;

namespace AssignmentSubmissionSystem.Application.Assignments;

public interface IAssignmentRepository
{
    Task AddAsync(Assignment assignment, CancellationToken cancellationToken);
    Task DeleteAsync(Assignment assignment, CancellationToken cancellationToken);
    Task<IReadOnlyList<Assignment>> GetForTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken);
    Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HasSubmissionsAsync(Guid assignmentId, CancellationToken cancellationToken);
    Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken);
}
