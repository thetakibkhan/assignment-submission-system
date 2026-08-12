using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Domain.Assignments;
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
    public Task<bool> HasSubmissionsAsync(Guid assignmentId, CancellationToken cancellationToken) => Task.FromResult(false);
    public async Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken) { _databaseContext.Assignments.Update(assignment); await _databaseContext.SaveChangesAsync(cancellationToken); }
}
