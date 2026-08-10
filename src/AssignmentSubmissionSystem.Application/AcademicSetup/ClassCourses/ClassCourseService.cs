using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.ClassCourses;

public sealed class ClassCourseService : IClassCourseService
{
    private readonly IClassCourseRepository _classCourseRepository;

    public ClassCourseService(IClassCourseRepository classCourseRepository)
    {
        _classCourseRepository = classCourseRepository;
    }

    public async Task<ClassCourse> CreateAsync(
        CreateClassCourseCommand command,
        CancellationToken cancellationToken)
    {
        string code = command.Code.Trim().ToUpperInvariant();

        if (await _classCourseRepository.ExistsByCodeAsync(code, cancellationToken))
        {
            throw new DuplicateClassCourseCodeException(code);
        }

        ClassCourse classCourse = new(Guid.CreateVersion7(), command.Name, code);
        await _classCourseRepository.AddAsync(classCourse, cancellationToken);

        return classCourse;
    }
}
