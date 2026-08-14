namespace AssignmentSubmissionSystem.Domain.Submissions;

public sealed class SubmissionAttachment
{
    public SubmissionAttachment(Guid id, Guid submissionId, string fileName, string contentType, string storageName)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(submissionId, Guid.Empty);
        Id = id;
        SubmissionId = submissionId;
        FileName = Path.GetFileName(fileName);
        ContentType = contentType;
        StorageName = storageName;
    }

    public Guid Id { get; private set; }
    public Guid SubmissionId { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public string StorageName { get; private set; }
}
