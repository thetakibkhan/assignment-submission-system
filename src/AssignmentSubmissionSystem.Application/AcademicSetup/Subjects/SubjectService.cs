using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.Subjects;

public sealed class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;

    public SubjectService(ISubjectRepository subjectRepository)
    {
        _subjectRepository = subjectRepository;
    }

    public async Task<Subject> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        Subject subject = await GetRequiredAsync(id, cancellationToken);
        subject.Archive();
        await _subjectRepository.UpdateAsync(subject, cancellationToken);

        return subject;
    }

    public async Task<Subject> CreateAsync(
        CreateSubjectCommand command,
        CancellationToken cancellationToken)
    {
        string code = command.Code.Trim().ToUpperInvariant();

        if (await _subjectRepository.ExistsByCodeAsync(code, cancellationToken))
        {
            throw new DuplicateSubjectCodeException(code);
        }

        Subject subject = new(Guid.CreateVersion7(), command.Name, code);
        await _subjectRepository.AddAsync(subject, cancellationToken);

        return subject;
    }

    public Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _subjectRepository.GetAllAsync(cancellationToken);
    }

    public async Task<Subject> UpdateAsync(
        Guid id,
        CreateSubjectCommand command,
        CancellationToken cancellationToken)
    {
        Subject subject = await GetRequiredAsync(id, cancellationToken);
        string code = command.Code.Trim().ToUpperInvariant();

        if (subject.Code != code && await _subjectRepository.ExistsByCodeAsync(code, cancellationToken))
        {
            throw new DuplicateSubjectCodeException(code);
        }

        subject.Update(command.Name, code);
        await _subjectRepository.UpdateAsync(subject, cancellationToken);

        return subject;
    }

    private async Task<Subject> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _subjectRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("The requested Subject was not found.");
    }
}
