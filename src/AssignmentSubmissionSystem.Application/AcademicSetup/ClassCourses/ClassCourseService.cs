using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.ClassCourses;

public sealed class ClassCourseService : IClassCourseService
{
    private readonly IClassCourseRepository _classCourseRepository;

    public ClassCourseService(IClassCourseRepository classCourseRepository)
    {
        _classCourseRepository = classCourseRepository;
    }

    public async Task<ClassCourse> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        ClassCourse classCourse = await GetRequiredAsync(id, cancellationToken);
        classCourse.Archive();
        await _classCourseRepository.UpdateAsync(classCourse, cancellationToken);
        return classCourse;
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

    public async Task<ClassCourse> UpdateAsync(Guid id, CreateClassCourseCommand command, CancellationToken cancellationToken)
    {
        ClassCourse classCourse = await GetRequiredAsync(id, cancellationToken);
        string code = command.Code.Trim().ToUpperInvariant();

        if (classCourse.Code != code && await _classCourseRepository.ExistsByCodeAsync(code, cancellationToken))
        {
            throw new DuplicateClassCourseCodeException(code);
        }

        classCourse.Update(command.Name, code);
        await _classCourseRepository.UpdateAsync(classCourse, cancellationToken);
        return classCourse;
    }

    private async Task<ClassCourse> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _classCourseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("The requested Class/Course was not found.");
    }
}
