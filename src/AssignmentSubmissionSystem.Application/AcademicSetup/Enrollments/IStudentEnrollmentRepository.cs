using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.Enrollments;

public interface IStudentEnrollmentRepository
{
    Task AddAsync(StudentEnrollment enrollment, CancellationToken cancellationToken);

    Task<bool> ExistsActiveAsync(Guid studentUserId, Guid classCourseId, CancellationToken cancellationToken);

    Task<StudentEnrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateAsync(StudentEnrollment enrollment, CancellationToken cancellationToken);
}
