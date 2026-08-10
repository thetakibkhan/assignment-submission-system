using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Domain.UnitTests.Academics;

public sealed class AcademicSetupTests
{
    [Fact]
    public void Archive_ShouldPreserveClassCourseIdentity()
    {
        Guid classCourseId = Guid.CreateVersion7();
        ClassCourse classCourse = new(classCourseId, "Class Nine", "CLS-9");

        classCourse.Archive();

        Assert.True(classCourse.IsArchived);
        Assert.Equal(classCourseId, classCourse.Id);
    }

    [Fact]
    public void End_ShouldPreserveEnrollmentHistory()
    {
        DateTimeOffset enrolledAt = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset endedAt = enrolledAt.AddDays(1);
        Guid studentUserId = Guid.CreateVersion7();
        Guid classCourseId = Guid.CreateVersion7();
        Guid enrolledByUserId = Guid.CreateVersion7();
        Guid endedByUserId = Guid.CreateVersion7();
        StudentEnrollment enrollment = new(
            Guid.CreateVersion7(),
            studentUserId,
            classCourseId,
            enrolledByUserId,
            enrolledAt);

        enrollment.End(endedByUserId, endedAt);

        Assert.False(enrollment.IsActive);
        Assert.Equal(studentUserId, enrollment.StudentUserId);
        Assert.Equal(classCourseId, enrollment.ClassCourseId);
        Assert.Equal(endedByUserId, enrollment.EndedByUserId);
        Assert.Equal(endedAt, enrollment.EndedAt);
    }

    [Fact]
    public void Revoke_ShouldPreserveTeacherResponsibilityHistory()
    {
        DateTimeOffset assignedAt = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset revokedAt = assignedAt.AddDays(1);
        Guid teacherUserId = Guid.CreateVersion7();
        Guid classCourseId = Guid.CreateVersion7();
        Guid subjectId = Guid.CreateVersion7();
        Guid assignedByUserId = Guid.CreateVersion7();
        Guid revokedByUserId = Guid.CreateVersion7();
        TeacherResponsibility responsibility = new(
            Guid.CreateVersion7(),
            teacherUserId,
            classCourseId,
            subjectId,
            assignedByUserId,
            assignedAt);

        responsibility.Revoke(revokedByUserId, revokedAt);

        Assert.False(responsibility.IsActive);
        Assert.Equal(teacherUserId, responsibility.TeacherUserId);
        Assert.Equal(classCourseId, responsibility.ClassCourseId);
        Assert.Equal(subjectId, responsibility.SubjectId);
        Assert.Equal(revokedByUserId, responsibility.RevokedByUserId);
        Assert.Equal(revokedAt, responsibility.RevokedAt);
    }
}
