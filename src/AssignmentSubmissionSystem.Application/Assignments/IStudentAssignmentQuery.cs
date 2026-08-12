namespace AssignmentSubmissionSystem.Application.Assignments;

public interface IStudentAssignmentQuery
{
    Task<IReadOnlyList<StudentAssignmentItem>> GetAllAsync(Guid studentUserId, DateTimeOffset currentTime, CancellationToken cancellationToken);
    Task<StudentAssignmentItem?> GetByIdAsync(Guid id, Guid studentUserId, DateTimeOffset currentTime, CancellationToken cancellationToken);
}
