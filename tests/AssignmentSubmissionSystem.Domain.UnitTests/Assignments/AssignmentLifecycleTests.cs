using AssignmentSubmissionSystem.Domain.Assignments;

namespace AssignmentSubmissionSystem.Domain.UnitTests.Assignments;

public sealed class AssignmentLifecycleTests
{
    [Fact]
    public void Publish_ShouldChangeDraftToPublished_WhenRequiredDetailsAreValid()
    {
        Assignment assignment = CreateDraft();

        assignment.Publish(DateTimeOffset.UtcNow);

        Assert.Equal(AssignmentStatus.Published, assignment.Status);
        Assert.NotNull(assignment.PublishedAt);
    }

    [Fact]
    public void Publish_ShouldRejectDraft_WhenDescriptionIsMissing()
    {
        Assignment assignment = new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Essay outline",
            null,
            null,
            null,
            null,
            DateTimeOffset.UtcNow);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => assignment.Publish(DateTimeOffset.UtcNow));

        Assert.Equal("A description is required before publishing an assignment.", exception.Message);
    }

    [Fact]
    public void Unpublish_ShouldReturnPublishedAssignmentToDraft_WhenNoSubmissionsExist()
    {
        Assignment assignment = CreateDraft();
        assignment.Publish(DateTimeOffset.UtcNow);

        assignment.Unpublish(false);

        Assert.Equal(AssignmentStatus.Draft, assignment.Status);
        Assert.Null(assignment.PublishedAt);
    }

    [Fact]
    public void Unpublish_ShouldRejectAssignment_WhenSubmissionsExist()
    {
        Assignment assignment = CreateDraft();
        assignment.Publish(DateTimeOffset.UtcNow);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => assignment.Unpublish(true));

        Assert.Equal("An assignment with submissions cannot return to Draft.", exception.Message);
    }

    private static Assignment CreateDraft()
    {
        return new Assignment(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Essay outline",
            "Write a concise outline for the assigned essay topic.",
            DateTimeOffset.UtcNow.AddDays(7),
            20m,
            true,
            DateTimeOffset.UtcNow);
    }
}
