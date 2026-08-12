using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Domain.Assignments;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Assignments;

public sealed class StudentAssignmentQuery : IStudentAssignmentQuery
{
    private readonly ApplicationDbContext _databaseContext;

    public StudentAssignmentQuery(ApplicationDbContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task<IReadOnlyList<StudentAssignmentItem>> GetAllAsync(Guid studentUserId, DateTimeOffset currentTime, CancellationToken cancellationToken)
    {
        return await CreateAuthorizedQuery(studentUserId, currentTime)
            .OrderBy(assignment => assignment.DeadlinePassed)
            .ThenBy(assignment => assignment.DeadlinePassed ? DateTimeOffset.MaxValue : assignment.Deadline)
            .ThenByDescending(assignment => assignment.DeadlinePassed ? assignment.Deadline : DateTimeOffset.MinValue)
            .ToListAsync(cancellationToken);
    }

    public Task<StudentAssignmentItem?> GetByIdAsync(Guid id, Guid studentUserId, DateTimeOffset currentTime, CancellationToken cancellationToken)
    {
        return CreateAuthorizedQuery(studentUserId, currentTime)
            .SingleOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);
    }

    private IQueryable<StudentAssignmentItem> CreateAuthorizedQuery(Guid studentUserId, DateTimeOffset currentTime)
    {
        return from assignment in _databaseContext.Assignments.AsNoTracking()
               join enrollment in _databaseContext.StudentEnrollments.AsNoTracking()
                   on assignment.ClassCourseId equals enrollment.ClassCourseId
               join classCourse in _databaseContext.ClassCourses.AsNoTracking()
                   on assignment.ClassCourseId equals classCourse.Id
               join subject in _databaseContext.Subjects.AsNoTracking()
                   on assignment.SubjectId equals subject.Id
               join teacher in _databaseContext.Users.AsNoTracking()
                   on assignment.TeacherUserId equals teacher.Id
               where assignment.Status == AssignmentStatus.Published
                   && enrollment.StudentUserId == studentUserId
                   && enrollment.EndedAt == null
               select new StudentAssignmentItem
               {
                   AllowSubmissionUpdates = assignment.AllowSubmissionUpdates ?? false,
                   ClassCourseName = classCourse.Name,
                   Deadline = assignment.Deadline ?? DateTimeOffset.MaxValue,
                   DeadlinePassed = assignment.Deadline <= currentTime,
                   Description = assignment.Description ?? string.Empty,
                   Id = assignment.Id,
                   MaximumMarks = assignment.MaximumMarks ?? 0m,
                   StudentState = "Not submitted",
                   Submission = null,
                   SubjectName = subject.Name,
                   TeacherName = teacher.FullName,
                   Title = assignment.Title
               };
    }
}
