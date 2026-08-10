using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.ClassCourses;

public interface IClassCourseService
{
    Task<ClassCourse> CreateAsync(CreateClassCourseCommand command, CancellationToken cancellationToken);
}
