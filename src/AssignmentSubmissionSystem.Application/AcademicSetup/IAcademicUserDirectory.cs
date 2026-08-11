namespace AssignmentSubmissionSystem.Application.AcademicSetup;

public interface IAcademicUserDirectory
{
    Task<Guid?> GetActiveStudentIdAsync(string institutionalId, CancellationToken cancellationToken);

    Task<Guid?> GetActiveTeacherIdAsync(string institutionalId, CancellationToken cancellationToken);
}
