using AssignmentSubmissionSystem.Application.Submissions;
using AssignmentSubmissionSystem.Domain.Submissions;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Submissions;

public sealed class SubmissionRepository : ISubmissionRepository
{
    private readonly ApplicationDbContext _databaseContext;

    public SubmissionRepository(ApplicationDbContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task AddAsync(Submission submission, CancellationToken cancellationToken)
    {
        await _databaseContext.Submissions.AddAsync(submission, cancellationToken);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Submission?> GetByAssignmentAndStudentAsync(Guid assignmentId, Guid studentUserId, CancellationToken cancellationToken)
    {
        return _databaseContext.Submissions.SingleOrDefaultAsync(
            submission => submission.AssignmentId == assignmentId && submission.StudentUserId == studentUserId,
            cancellationToken);
    }

    public async Task UpdateAsync(Submission submission, CancellationToken cancellationToken)
    {
        _databaseContext.Submissions.Update(submission);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }
}
