using AssignmentSubmissionSystem.Application.Dashboards;
using AssignmentSubmissionSystem.Domain.Assignments;
using AssignmentSubmissionSystem.Domain.Submissions;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Dashboards;

public sealed class DashboardQuery : IDashboardQuery
{
    private readonly ApplicationDbContext _databaseContext;

    public DashboardQuery(ApplicationDbContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task<AdminDashboardSummary> GetAdminAsync(CancellationToken cancellationToken)
    {
        return new AdminDashboardSummary
        {
            ActiveAccounts = await _databaseContext.Users.CountAsync(user => user.IsActive, cancellationToken),
            ActiveClassCourses = await _databaseContext.ClassCourses.CountAsync(classCourse => !classCourse.IsArchived, cancellationToken),
            ActiveSubjects = await _databaseContext.Subjects.CountAsync(subject => !subject.IsArchived, cancellationToken),
            AssignmentCount = await _databaseContext.Assignments.CountAsync(cancellationToken),
            SubmissionCount = await _databaseContext.Submissions.CountAsync(cancellationToken)
        };
    }

    public async Task<TeacherDashboardSummary> GetTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken)
    {
        return new TeacherDashboardSummary
        {
            DraftAssignments = await _databaseContext.Assignments.CountAsync(assignment => assignment.TeacherUserId == teacherUserId && assignment.Status == AssignmentStatus.Draft, cancellationToken),
            PublishedAssignments = await _databaseContext.Assignments.CountAsync(assignment => assignment.TeacherUserId == teacherUserId && assignment.Status == AssignmentStatus.Published, cancellationToken),
            SubmissionsNeedingReview = await (from submission in _databaseContext.Submissions
                                              join assignment in _databaseContext.Assignments on submission.AssignmentId equals assignment.Id
                                              where assignment.TeacherUserId == teacherUserId && submission.Status == SubmissionStatus.Submitted
                                              select submission).CountAsync(cancellationToken),
            SubmissionsUnderReview = await (from submission in _databaseContext.Submissions
                                            join assignment in _databaseContext.Assignments on submission.AssignmentId equals assignment.Id
                                            where assignment.TeacherUserId == teacherUserId && submission.Status == SubmissionStatus.UnderReview
                                            select submission).CountAsync(cancellationToken)
        };
    }

    public async Task<StudentDashboardSummary> GetStudentAsync(Guid studentUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        IQueryable<Guid> openAssignmentIds = from assignment in _databaseContext.Assignments.AsNoTracking()
                                             join enrollment in _databaseContext.StudentEnrollments.AsNoTracking() on assignment.ClassCourseId equals enrollment.ClassCourseId
                                             where enrollment.StudentUserId == studentUserId && enrollment.EndedAt == null && assignment.Status == AssignmentStatus.Published && assignment.Deadline > now
                                             select assignment.Id;
        return new StudentDashboardSummary
        {
            GradedSubmissions = await _databaseContext.Submissions.CountAsync(submission => submission.StudentUserId == studentUserId && submission.Status == SubmissionStatus.Graded, cancellationToken),
            NearestDeadline = await (from assignment in _databaseContext.Assignments.AsNoTracking()
                                     join enrollment in _databaseContext.StudentEnrollments.AsNoTracking() on assignment.ClassCourseId equals enrollment.ClassCourseId
                                     where enrollment.StudentUserId == studentUserId && enrollment.EndedAt == null && assignment.Status == AssignmentStatus.Published && assignment.Deadline > now
                                     orderby assignment.Deadline
                                     select assignment.Deadline).FirstOrDefaultAsync(cancellationToken),
            OpenAssignments = await openAssignmentIds.CountAsync(cancellationToken),
            SubmittedAssignments = await _databaseContext.Submissions.CountAsync(submission => submission.StudentUserId == studentUserId && submission.Status != SubmissionStatus.Graded, cancellationToken)
        };
    }
}
