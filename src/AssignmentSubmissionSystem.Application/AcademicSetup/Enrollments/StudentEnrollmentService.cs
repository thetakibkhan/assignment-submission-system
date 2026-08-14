using AssignmentSubmissionSystem.Application.AcademicSetup.AcademicClasses;
using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.Enrollments;

public sealed class StudentEnrollmentService : IStudentEnrollmentService
{
    private readonly IAcademicUserDirectory _academicUserDirectory;
    private readonly IAcademicClassRepository _academicClassRepository;
    private readonly IStudentEnrollmentRepository _studentEnrollmentRepository;

    public StudentEnrollmentService(
        IAcademicUserDirectory academicUserDirectory,
        IAcademicClassRepository academicClassRepository,
        IStudentEnrollmentRepository studentEnrollmentRepository)
    {
        _academicUserDirectory = academicUserDirectory;
        _academicClassRepository = academicClassRepository;
        _studentEnrollmentRepository = studentEnrollmentRepository;
    }

    public async Task<StudentEnrollment> CreateAsync(
        CreateStudentEnrollmentCommand command,
        Guid enrolledByUserId,
        CancellationToken cancellationToken)
    {
        AcademicClass academicClass = await _academicClassRepository.GetByIdAsync(command.AcademicClassId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested Class was not found.");

        if (academicClass.IsArchived)
        {
            throw new InvalidOperationException("An archived Class cannot accept new enrollments.");
        }

        Guid studentUserId = await _academicUserDirectory.GetActiveStudentIdAsync(
            command.StudentInstitutionalId,
            cancellationToken)
            ?? throw new ArgumentException("The requested active Student account was not found.", nameof(command));

        if (await _studentEnrollmentRepository.ExistsActiveAsync(
            studentUserId,
            command.AcademicClassId,
            cancellationToken))
        {
            throw new DuplicateActiveEnrollmentException();
        }

        StudentEnrollment enrollment = new(
            Guid.CreateVersion7(),
            studentUserId,
            command.AcademicClassId,
            enrolledByUserId,
            DateTimeOffset.UtcNow);
        await _studentEnrollmentRepository.AddAsync(enrollment, cancellationToken);

        return enrollment;
    }

    public async Task EndAsync(Guid id, Guid endedByUserId, CancellationToken cancellationToken)
    {
        StudentEnrollment enrollment = await _studentEnrollmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("The requested enrollment was not found.");
        enrollment.End(endedByUserId, DateTimeOffset.UtcNow);
        await _studentEnrollmentRepository.UpdateAsync(enrollment, cancellationToken);
    }
}
