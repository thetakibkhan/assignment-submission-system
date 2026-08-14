using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Domain.Assignments;
using AssignmentSubmissionSystem.Domain.Notifications;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Assignments;

public sealed class AssignmentRepository : IAssignmentRepository
{
    private readonly ApplicationDbContext _databaseContext;

    public AssignmentRepository(ApplicationDbContext databaseContext) => _databaseContext = databaseContext;
    public async Task AddAsync(Assignment assignment, CancellationToken cancellationToken) { await _databaseContext.Assignments.AddAsync(assignment, cancellationToken); await _databaseContext.SaveChangesAsync(cancellationToken); }
    public async Task DeleteAsync(Assignment assignment, CancellationToken cancellationToken) { _databaseContext.Assignments.Remove(assignment); await _databaseContext.SaveChangesAsync(cancellationToken); }
    public async Task<IReadOnlyList<Assignment>> GetForTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken) => await _databaseContext.Assignments.AsNoTracking().Where(assignment => assignment.TeacherUserId == teacherUserId).OrderByDescending(assignment => assignment.UpdatedAt).ToListAsync(cancellationToken);
    public Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => _databaseContext.Assignments.SingleOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);
    public Task<bool> HasSubmissionsAsync(Guid assignmentId, CancellationToken cancellationToken) => _databaseContext.Submissions.AnyAsync(submission => submission.AssignmentId == assignmentId, cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetIdsWithSubmissionsAsync(
        IReadOnlyCollection<Guid> assignmentIds,
        CancellationToken cancellationToken)
    {
        if (assignmentIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        List<Guid> assignmentIdsWithSubmissions = await _databaseContext.Submissions
            .AsNoTracking()
            .Where(submission => assignmentIds.Contains(submission.AssignmentId))
            .Select(submission => submission.AssignmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return assignmentIdsWithSubmissions.ToHashSet();
    }
    public async Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken) { _databaseContext.Assignments.Update(assignment); await _databaseContext.SaveChangesAsync(cancellationToken); }
    public async Task UpdateWithNotificationsAsync(Assignment assignment, IReadOnlyList<UserNotification> notifications, CancellationToken cancellationToken) { _databaseContext.Assignments.Update(assignment); await _databaseContext.UserNotifications.AddRangeAsync(notifications, cancellationToken); await _databaseContext.SaveChangesAsync(cancellationToken); }
}
