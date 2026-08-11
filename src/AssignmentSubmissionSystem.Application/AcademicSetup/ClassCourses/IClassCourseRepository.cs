using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.ClassCourses;

public interface IClassCourseRepository
{
    Task AddAsync(ClassCourse classCourse, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClassCourse>> GetAllAsync(CancellationToken cancellationToken);

    Task<ClassCourse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateAsync(ClassCourse classCourse, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);
}
