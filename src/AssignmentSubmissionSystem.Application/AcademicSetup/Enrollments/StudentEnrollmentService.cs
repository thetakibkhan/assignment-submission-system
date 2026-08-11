using AssignmentSubmissionSystem.Application.AcademicSetup.ClassCourses;
using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.Enrollments;

public sealed class StudentEnrollmentService : IStudentEnrollmentService
{
    private readonly IAcademicUserDirectory _academicUserDirectory;
    private readonly IClassCourseRepository _classCourseRepository;
    private readonly IStudentEnrollmentRepository _studentEnrollmentRepository;

    public StudentEnrollmentService(
        IAcademicUserDirectory academicUserDirectory,
        IClassCourseRepository classCourseRepository,
        IStudentEnrollmentRepository studentEnrollmentRepository)
    {
        _academicUserDirectory = academicUserDirectory;
        _classCourseRepository = classCourseRepository;
        _studentEnrollmentRepository = studentEnrollmentRepository;
    }

    public async Task<StudentEnrollment> CreateAsync(
        CreateStudentEnrollmentCommand command,
        Guid enrolledByUserId,
        CancellationToken cancellationToken)
    {
        ClassCourse classCourse = await _classCourseRepository.GetByIdAsync(command.ClassCourseId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested Class/Course was not found.");

        if (classCourse.IsArchived)
        {
            throw new InvalidOperationException("An archived Class/Course cannot accept new enrollments.");
        }

        Guid studentUserId = await _academicUserDirectory.GetActiveStudentIdAsync(
            command.StudentInstitutionalId,
            cancellationToken)
            ?? throw new ArgumentException("The requested active Student account was not found.", nameof(command));

        if (await _studentEnrollmentRepository.ExistsActiveAsync(
            studentUserId,
            command.ClassCourseId,
            cancellationToken))
        {
            throw new DuplicateActiveEnrollmentException();
        }

        StudentEnrollment enrollment = new(
            Guid.CreateVersion7(),
            studentUserId,
            command.ClassCourseId,
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
