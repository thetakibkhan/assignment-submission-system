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
        try
        {
            await _databaseContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new InvalidOperationException("You have already submitted work for this assignment.", exception);
        }
    }

    public Task<Submission?> GetByIdForTeacherAsync(
        Guid submissionId,
        Guid teacherUserId,
        CancellationToken cancellationToken)
    {
        return (from submission in _databaseContext.Submissions
                join assignment in _databaseContext.Assignments on submission.AssignmentId equals assignment.Id
                where submission.Id == submissionId && assignment.TeacherUserId == teacherUserId
                select submission).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Submission?> GetByIdAndStudentAsync(
        Guid submissionId,
        Guid studentUserId,
        CancellationToken cancellationToken)
    {
        return _databaseContext.Submissions.SingleOrDefaultAsync(
            submission => submission.Id == submissionId && submission.StudentUserId == studentUserId,
            cancellationToken);
    }

    public Task<Submission?> GetByAssignmentAndStudentAsync(Guid assignmentId, Guid studentUserId, CancellationToken cancellationToken)
    {
        return _databaseContext.Submissions.SingleOrDefaultAsync(
            submission => submission.AssignmentId == assignmentId && submission.StudentUserId == studentUserId,
            cancellationToken);
    }

    public async Task UpdateWithReviewRevisionAsync(
        Submission submission,
        SubmissionReviewRevision revision,
        CancellationToken cancellationToken)
    {
        _databaseContext.SubmissionReviewRevisions.Add(revision);
        _databaseContext.Submissions.Update(submission);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateWithRevisionAsync(
        Submission submission,
        SubmissionRevision revision,
        CancellationToken cancellationToken)
    {
        _databaseContext.SubmissionRevisions.Add(revision);
        _databaseContext.Submissions.Update(submission);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }
}
