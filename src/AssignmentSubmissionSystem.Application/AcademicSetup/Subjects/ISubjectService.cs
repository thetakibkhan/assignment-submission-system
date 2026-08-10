using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.Subjects;

public interface ISubjectService
{
    Task<Subject> ArchiveAsync(Guid id, CancellationToken cancellationToken);

    Task<Subject> CreateAsync(CreateSubjectCommand command, CancellationToken cancellationToken);

    Task<Subject> UpdateAsync(Guid id, CreateSubjectCommand command, CancellationToken cancellationToken);
}
