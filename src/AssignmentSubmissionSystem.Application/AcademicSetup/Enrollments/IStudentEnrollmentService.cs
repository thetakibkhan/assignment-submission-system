using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.Enrollments;

public interface IStudentEnrollmentService
{
    Task<StudentEnrollment> CreateAsync(
        CreateStudentEnrollmentCommand command,
        Guid enrolledByUserId,
        CancellationToken cancellationToken);

    Task EndAsync(Guid id, Guid endedByUserId, CancellationToken cancellationToken);
}
