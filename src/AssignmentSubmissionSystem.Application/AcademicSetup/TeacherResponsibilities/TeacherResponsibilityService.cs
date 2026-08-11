using AssignmentSubmissionSystem.Application.AcademicSetup.ClassCourses;
using AssignmentSubmissionSystem.Application.AcademicSetup.Subjects;
using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.TeacherResponsibilities;

public sealed class TeacherResponsibilityService : ITeacherResponsibilityService
{
    private readonly IAcademicUserDirectory _academicUserDirectory;
    private readonly IClassCourseRepository _classCourseRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ITeacherResponsibilityRepository _teacherResponsibilityRepository;

    public TeacherResponsibilityService(
        IAcademicUserDirectory academicUserDirectory,
        IClassCourseRepository classCourseRepository,
        ISubjectRepository subjectRepository,
        ITeacherResponsibilityRepository teacherResponsibilityRepository)
    {
        _academicUserDirectory = academicUserDirectory;
        _classCourseRepository = classCourseRepository;
        _subjectRepository = subjectRepository;
        _teacherResponsibilityRepository = teacherResponsibilityRepository;
    }

    public async Task<TeacherResponsibility> CreateAsync(
        CreateTeacherResponsibilityCommand command,
        Guid assignedByUserId,
        CancellationToken cancellationToken)
    {
        ClassCourse classCourse = await _classCourseRepository.GetByIdAsync(command.ClassCourseId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested Class/Course was not found.");
        Subject subject = await _subjectRepository.GetByIdAsync(command.SubjectId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested Subject was not found.");

        if (classCourse.IsArchived || subject.IsArchived)
        {
            throw new InvalidOperationException("Archived Class/Courses and Subjects cannot receive Teacher responsibilities.");
        }

        Guid teacherUserId = await _academicUserDirectory.GetActiveTeacherIdAsync(
            command.TeacherInstitutionalId,
            cancellationToken)
            ?? throw new ArgumentException("The requested active Teacher account was not found.", nameof(command));

        if (await _teacherResponsibilityRepository.ExistsActiveAsync(
            command.ClassCourseId,
            command.SubjectId,
            cancellationToken))
        {
            throw new DuplicateActiveTeacherResponsibilityException();
        }

        TeacherResponsibility responsibility = new(
            Guid.CreateVersion7(),
            teacherUserId,
            command.ClassCourseId,
            command.SubjectId,
            assignedByUserId,
            DateTimeOffset.UtcNow);
        await _teacherResponsibilityRepository.AddAsync(responsibility, cancellationToken);

        return responsibility;
    }

    public async Task RevokeAsync(Guid id, Guid revokedByUserId, CancellationToken cancellationToken)
    {
        TeacherResponsibility responsibility = await _teacherResponsibilityRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("The requested Teacher responsibility was not found.");
        responsibility.Revoke(revokedByUserId, DateTimeOffset.UtcNow);
        await _teacherResponsibilityRepository.UpdateAsync(responsibility, cancellationToken);
    }
}
