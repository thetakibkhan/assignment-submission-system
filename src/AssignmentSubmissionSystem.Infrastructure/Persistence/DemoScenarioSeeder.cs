using AssignmentSubmissionSystem.Domain.Academics;
using AssignmentSubmissionSystem.Domain.Assignments;
using AssignmentSubmissionSystem.Domain.Notifications;
using AssignmentSubmissionSystem.Domain.Submissions;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence;

public sealed class DemoScenarioSeeder
{
    public const string HistoricalResultTitle = "Historical reading response";
    public const string OpenAssignmentTitle = "Argument essay";

    private const string DraftAssignmentTitle = "Literature reflection";
    private const string ReviewAssignmentTitle = "Science observation notes";
    private readonly ApplicationDbContext _databaseContext;

    public DemoScenarioSeeder(ApplicationDbContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task EnsureAsync(
        ApplicationUser administrator,
        ApplicationUser teacher,
        ApplicationUser student,
        CancellationToken cancellationToken)
    {
        ClassCourse classCourse = await _databaseContext.ClassCourses.SingleAsync(
            item => item.Code == "CLS-10",
            cancellationToken);
        Subject subject = await _databaseContext.Subjects.SingleAsync(
            item => item.Code == "SUB-ENG",
            cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (!await _databaseContext.TeacherResponsibilities.AnyAsync(
            item => item.TeacherUserId == teacher.Id
                && item.ClassCourseId == classCourse.Id
                && item.SubjectId == subject.Id
                && item.RevokedAt == null,
            cancellationToken))
        {
            _databaseContext.TeacherResponsibilities.Add(new TeacherResponsibility(
                Guid.CreateVersion7(),
                teacher.Id,
                classCourse.Id,
                subject.Id,
                administrator.Id,
                now));
        }

        if (!await _databaseContext.StudentEnrollments.AnyAsync(
            item => item.StudentUserId == student.Id
                && item.ClassCourseId == classCourse.Id
                && item.EndedAt == null,
            cancellationToken))
        {
            _databaseContext.StudentEnrollments.Add(new StudentEnrollment(
                Guid.CreateVersion7(),
                student.Id,
                classCourse.Id,
                administrator.Id,
                now));
        }

        Assignment draft = await EnsureAssignmentAsync(
            teacher, classCourse, subject, DraftAssignmentTitle,
            "Write a short reflection on the assigned literature extract.",
            now.AddDays(7), now, false, cancellationToken);
        Assignment open = await EnsureAssignmentAsync(
            teacher, classCourse, subject, OpenAssignmentTitle,
            "Write a concise argument with a clear claim and supporting evidence.",
            now.AddDays(7), now, true, cancellationToken);
        Assignment review = await EnsureAssignmentAsync(
            teacher, classCourse, subject, ReviewAssignmentTitle,
            "Record observations and explain the scientific pattern you found.",
            now.AddDays(5), now, false, cancellationToken);
        Assignment historical = await EnsureAssignmentAsync(
            teacher, classCourse, subject, HistoricalResultTitle,
            "Summarize the reading and explain its central idea.",
            now.AddDays(-7), now.AddDays(-30), false, cancellationToken);

        await EnsureSubmissionAsync(
            review,
            student,
            "My observation notes are ready for review.",
            false,
            now.AddDays(-1),
            cancellationToken);
        Submission historicalSubmission = await EnsureSubmissionAsync(
            historical,
            student,
            "The text shows how careful reading changes interpretation.",
            true,
            now.AddDays(-14),
            cancellationToken);
        await EnsureNotificationAsync(
            student.Id,
            NotificationType.AssignmentPublished,
            open.Id,
            null,
            "A new assignment is available: " + open.Title,
            now,
            cancellationToken);
        await EnsureNotificationAsync(
            student.Id,
            NotificationType.SubmissionGraded,
            historical.Id,
            historicalSubmission.Id,
            "Your submission has been graded. Results are visible after the deadline.",
            now,
            cancellationToken);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Assignment> EnsureAssignmentAsync(
        ApplicationUser teacher,
        ClassCourse classCourse,
        Subject subject,
        string title,
        string description,
        DateTimeOffset deadline,
        DateTimeOffset publishedAt,
        bool allowUpdates,
        CancellationToken cancellationToken)
    {
        Assignment? assignment = await _databaseContext.Assignments.SingleOrDefaultAsync(
            item => item.TeacherUserId == teacher.Id && item.Title == title,
            cancellationToken);
        if (assignment is not null)
        {
            return assignment;
        }

        assignment = new Assignment(
            Guid.CreateVersion7(),
            teacher.Id,
            classCourse.Id,
            subject.Id,
            title,
            description,
            deadline,
            100m,
            allowUpdates,
            publishedAt);
        if (title != DraftAssignmentTitle)
        {
            assignment.Publish(publishedAt);
        }

        _databaseContext.Assignments.Add(assignment);
        return assignment;
    }

    private async Task<Submission> EnsureSubmissionAsync(
        Assignment assignment,
        ApplicationUser student,
        string textAnswer,
        bool isGraded,
        DateTimeOffset submittedAt,
        CancellationToken cancellationToken)
    {
        Submission? submission = await _databaseContext.Submissions.SingleOrDefaultAsync(
            item => item.AssignmentId == assignment.Id && item.StudentUserId == student.Id,
            cancellationToken);
        if (submission is not null)
        {
            return submission;
        }

        submission = new Submission(
            Guid.CreateVersion7(),
            assignment.Id,
            student.Id,
            textAnswer,
            submittedAt);
        if (isGraded)
        {
            submission.StartReview();
            submission.UpdateReview(88m, "Clear reasoning and a well-supported interpretation.");
            submission.Grade();
        }

        _databaseContext.Submissions.Add(submission);
        return submission;
    }

    private async Task EnsureNotificationAsync(
        Guid recipientUserId,
        NotificationType type,
        Guid assignmentId,
        Guid? submissionId,
        string message,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        bool exists = await _databaseContext.UserNotifications.AnyAsync(
            notification => notification.RecipientUserId == recipientUserId
                && notification.Type == type
                && notification.AssignmentId == assignmentId,
            cancellationToken);
        if (!exists)
        {
            _databaseContext.UserNotifications.Add(new UserNotification(
                Guid.CreateVersion7(),
                recipientUserId,
                type,
                assignmentId,
                submissionId,
                message,
                createdAt));
        }
    }
}
