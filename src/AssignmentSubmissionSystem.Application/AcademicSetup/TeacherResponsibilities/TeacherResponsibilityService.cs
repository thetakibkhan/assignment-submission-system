using AssignmentSubmissionSystem.Application.AcademicSetup.AcademicClasses;
using AssignmentSubmissionSystem.Application.AcademicSetup.Subjects;
using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.TeacherResponsibilities;

public sealed class TeacherResponsibilityService : ITeacherResponsibilityService
{
    private readonly IAcademicUserDirectory _academicUserDirectory;
    private readonly IAcademicClassRepository _academicClassRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ITeacherResponsibilityRepository _teacherResponsibilityRepository;

    public TeacherResponsibilityService(
        IAcademicUserDirectory academicUserDirectory,
        IAcademicClassRepository academicClassRepository,
        ISubjectRepository subjectRepository,
        ITeacherResponsibilityRepository teacherResponsibilityRepository)
    {
        _academicUserDirectory = academicUserDirectory;
        _academicClassRepository = academicClassRepository;
        _subjectRepository = subjectRepository;
        _teacherResponsibilityRepository = teacherResponsibilityRepository;
    }

    public async Task<TeacherResponsibility> CreateAsync(
        CreateTeacherResponsibilityCommand command,
        Guid assignedByUserId,
        CancellationToken cancellationToken)
    {
        AcademicClass academicClass = await _academicClassRepository.GetByIdAsync(command.AcademicClassId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested Class was not found.");
        Subject subject = await _subjectRepository.GetByIdAsync(command.SubjectId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested Subject was not found.");

        if (academicClass.IsArchived || subject.IsArchived)
        {
            throw new InvalidOperationException("Archived Classes and Subjects cannot receive Teacher responsibilities.");
        }

        Guid teacherUserId = await _academicUserDirectory.GetActiveTeacherIdAsync(
            command.TeacherInstitutionalId,
            cancellationToken)
            ?? throw new ArgumentException("The requested active Teacher account was not found.", nameof(command));

        if (await _teacherResponsibilityRepository.ExistsActiveAsync(
            command.AcademicClassId,
            command.SubjectId,
            cancellationToken))
        {
            throw new DuplicateActiveTeacherResponsibilityException();
        }

        TeacherResponsibility responsibility = new(
            Guid.CreateVersion7(),
            teacherUserId,
            command.AcademicClassId,
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
