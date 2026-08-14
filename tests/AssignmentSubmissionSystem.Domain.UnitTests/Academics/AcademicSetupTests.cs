using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Domain.UnitTests.Academics;

public sealed class AcademicSetupTests
{
    [Fact]
    public void Archive_ShouldPreserveAcademicClassIdentity()
    {
        Guid academicClassId = Guid.CreateVersion7();
        AcademicClass academicClass = new(academicClassId, "Class Nine", "CLS-9");

        academicClass.Archive();

        Assert.True(academicClass.IsArchived);
        Assert.Equal(academicClassId, academicClass.Id);
    }

    [Fact]
    public void End_ShouldPreserveEnrollmentHistory()
    {
        DateTimeOffset enrolledAt = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset endedAt = enrolledAt.AddDays(1);
        Guid studentUserId = Guid.CreateVersion7();
        Guid academicClassId = Guid.CreateVersion7();
        Guid enrolledByUserId = Guid.CreateVersion7();
        Guid endedByUserId = Guid.CreateVersion7();
        StudentEnrollment enrollment = new(
            Guid.CreateVersion7(),
            studentUserId,
            academicClassId,
            enrolledByUserId,
            enrolledAt);

        enrollment.End(endedByUserId, endedAt);

        Assert.False(enrollment.IsActive);
        Assert.Equal(studentUserId, enrollment.StudentUserId);
        Assert.Equal(academicClassId, enrollment.AcademicClassId);
        Assert.Equal(endedByUserId, enrollment.EndedByUserId);
        Assert.Equal(endedAt, enrollment.EndedAt);
    }

    [Fact]
    public void Revoke_ShouldPreserveTeacherResponsibilityHistory()
    {
        DateTimeOffset assignedAt = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset revokedAt = assignedAt.AddDays(1);
        Guid teacherUserId = Guid.CreateVersion7();
        Guid academicClassId = Guid.CreateVersion7();
        Guid subjectId = Guid.CreateVersion7();
        Guid assignedByUserId = Guid.CreateVersion7();
        Guid revokedByUserId = Guid.CreateVersion7();
        TeacherResponsibility responsibility = new(
            Guid.CreateVersion7(),
            teacherUserId,
            academicClassId,
            subjectId,
            assignedByUserId,
            assignedAt);

        responsibility.Revoke(revokedByUserId, revokedAt);

        Assert.False(responsibility.IsActive);
        Assert.Equal(teacherUserId, responsibility.TeacherUserId);
        Assert.Equal(academicClassId, responsibility.AcademicClassId);
        Assert.Equal(subjectId, responsibility.SubjectId);
        Assert.Equal(revokedByUserId, responsibility.RevokedByUserId);
        Assert.Equal(revokedAt, responsibility.RevokedAt);
    }
}
