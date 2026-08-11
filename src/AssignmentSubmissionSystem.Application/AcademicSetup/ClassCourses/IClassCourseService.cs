using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.ClassCourses;

public interface IClassCourseService
{
    Task<ClassCourse> ArchiveAsync(Guid id, CancellationToken cancellationToken);

    Task<ClassCourse> CreateAsync(CreateClassCourseCommand command, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClassCourse>> GetAllAsync(CancellationToken cancellationToken);

    Task<ClassCourse> UpdateAsync(Guid id, CreateClassCourseCommand command, CancellationToken cancellationToken);
}
