namespace AssignmentSubmissionSystem.Application.Dashboards;

public interface IDashboardQuery
{
    Task<AdminDashboardSummary> GetAdminAsync(CancellationToken cancellationToken);
    Task<StudentDashboardSummary> GetStudentAsync(Guid studentUserId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<TeacherDashboardSummary> GetTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken);
}
