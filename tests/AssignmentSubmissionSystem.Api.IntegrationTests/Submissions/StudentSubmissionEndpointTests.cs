using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.Submissions;

public sealed class StudentSubmissionEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public StudentSubmissionEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ShouldCreateSubmittedSubmission_WhenAssignmentIsEligibleAndOpen()
    {
        Guid assignmentId = await CreatePublishedAssignmentAsync(allowSubmissionUpdates: true);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");

        using MultipartFormDataContent content = CreateSubmissionContent("My completed response.");
        HttpResponseMessage response = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        SubmissionResponse? submission = await response.Content.ReadFromJsonAsync<SubmissionResponse>();

        Assert.NotNull(submission);
        Assert.Equal("My completed response.", submission.TextAnswer);
        Assert.Equal("Submitted", submission.Status);
    }

    [Fact]
    public async Task Create_ShouldRejectEmptySubmission()
    {
        Guid assignmentId = await CreatePublishedAssignmentAsync(allowSubmissionUpdates: true);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");
        using MultipartFormDataContent content = CreateSubmissionContent(null);

        HttpResponseMessage response = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldAllowAnAttachmentWithoutText()
    {
        Guid assignmentId = await CreatePublishedAssignmentAsync(allowSubmissionUpdates: true);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");
        using MultipartFormDataContent content = new();
        ByteArrayContent attachment = new("Submission attachment"u8.ToArray());
        attachment.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(attachment, "attachment", "response.txt");

        HttpResponseMessage response = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        SubmissionResponse? submission = await response.Content.ReadFromJsonAsync<SubmissionResponse>();
        Assert.NotNull(submission);
        Assert.Equal("response.txt", submission.AttachmentFileName);
    }

    [Fact]
    public async Task DownloadAttachment_ShouldReturnOnlyTheSubmittingStudentsFile()
    {
        Guid assignmentId = await CreatePublishedAssignmentAsync(allowSubmissionUpdates: true);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");
        using MultipartFormDataContent content = new();
        ByteArrayContent attachment = new("Private response"u8.ToArray());
        attachment.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(attachment, "attachment", "private.txt");
        HttpResponseMessage createResponse = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            content);
        createResponse.EnsureSuccessStatusCode();
        SubmissionResponse submission = await createResponse.Content.ReadFromJsonAsync<SubmissionResponse>()
            ?? throw new InvalidOperationException("The submission response was empty.");

        HttpResponseMessage response = await studentClient.GetAsync(
            "/api/student/submissions/" + submission.Id + "/attachment");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Private response", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_ShouldAllowTextAndAttachmentTogether()
    {
        Guid assignmentId = await CreatePublishedAssignmentAsync(allowSubmissionUpdates: true);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");
        using MultipartFormDataContent content = CreateSubmissionContent("Written answer");
        content.Add(new ByteArrayContent("Evidence"u8.ToArray()), "attachment", "evidence.txt");

        HttpResponseMessage response = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        SubmissionResponse? submission = await response.Content.ReadFromJsonAsync<SubmissionResponse>();
        Assert.NotNull(submission);
        Assert.Equal("Written answer", submission.TextAnswer);
        Assert.Equal("evidence.txt", submission.AttachmentFileName);
    }

    [Fact]
    public async Task Create_ShouldRejectDuplicateSubmission()
    {
        Guid assignmentId = await CreatePublishedAssignmentAsync(allowSubmissionUpdates: true);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");
        using MultipartFormDataContent firstContent = CreateSubmissionContent("First response");
        HttpResponseMessage firstResponse = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            firstContent);
        firstResponse.EnsureSuccessStatusCode();
        using MultipartFormDataContent duplicateContent = CreateSubmissionContent("Duplicate response");

        HttpResponseMessage duplicateResponse = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            duplicateContent);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldRejectSubmission_WhenDeadlineHasPassed()
    {
        Guid assignmentId = await CreatePublishedAssignmentAsync(allowSubmissionUpdates: true);
        await MoveDeadlineToPastAsync(assignmentId);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");
        using MultipartFormDataContent content = CreateSubmissionContent("Too late");

        HttpResponseMessage response = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            content);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldRejectSubmission_WhenAssignmentDisablesUpdates()
    {
        Guid assignmentId = await CreatePublishedAssignmentAsync(allowSubmissionUpdates: false);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");
        using MultipartFormDataContent firstContent = CreateSubmissionContent("Original response");
        HttpResponseMessage createResponse = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            firstContent);
        createResponse.EnsureSuccessStatusCode();
        using MultipartFormDataContent updatedContent = CreateSubmissionContent("Updated response");

        HttpResponseMessage response = await studentClient.PutAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            updatedContent);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldPreserveThePreviousContentAsAnImmutableRevision()
    {
        Guid assignmentId = await CreatePublishedAssignmentAsync(allowSubmissionUpdates: true);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");
        using MultipartFormDataContent originalContent = CreateSubmissionContent("Original response");
        HttpResponseMessage createResponse = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            originalContent);
        createResponse.EnsureSuccessStatusCode();
        SubmissionResponse submission = await createResponse.Content.ReadFromJsonAsync<SubmissionResponse>()
            ?? throw new InvalidOperationException("The submission response was empty.");
        using MultipartFormDataContent updatedContent = CreateSubmissionContent("Improved response");

        HttpResponseMessage updateResponse = await studentClient.PutAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            updatedContent);

        updateResponse.EnsureSuccessStatusCode();
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        string? originalText = await databaseContext.SubmissionRevisions
            .Where(revision => revision.SubmissionId == submission.Id)
            .Select(revision => revision.TextAnswer)
            .SingleAsync();
        Assert.Equal("Original response", originalText);
    }

    [Fact]
    public async Task Get_ShouldPreserveHistoricalAccessAfterEnrollmentEnds()
    {
        Guid assignmentId = await CreatePublishedAssignmentAsync(allowSubmissionUpdates: true);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");
        using MultipartFormDataContent content = CreateSubmissionContent("Historical response");
        HttpResponseMessage createResponse = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            content);
        createResponse.EnsureSuccessStatusCode();

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Guid classCourseId = await databaseContext.Assignments
            .Where(assignment => assignment.Id == assignmentId)
            .Select(assignment => assignment.ClassCourseId)
            .SingleAsync();
        await databaseContext.StudentEnrollments
            .Where(enrollment => enrollment.ClassCourseId == classCourseId && enrollment.EndedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                enrollment => enrollment.EndedAt,
                DateTimeOffset.UtcNow));

        HttpResponseMessage getResponse = await studentClient.GetAsync(
            "/api/student/assignments/" + assignmentId + "/submission");
        using MultipartFormDataContent updateContent = CreateSubmissionContent("Blocked update");
        HttpResponseMessage updateResponse = await studentClient.PutAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            updateContent);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
    }

    [Fact]
    public async Task Get_ShouldReturnTheCurrentSubmission_ForTheSubmittingStudent()
    {
        Guid assignmentId = await CreatePublishedAssignmentAsync(allowSubmissionUpdates: true);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");
        using MultipartFormDataContent content = CreateSubmissionContent("Stored response");
        HttpResponseMessage createResponse = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            content);
        createResponse.EnsureSuccessStatusCode();

        HttpResponseMessage response = await studentClient.GetAsync(
            "/api/student/assignments/" + assignmentId + "/submission");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SubmissionResponse? submission = await response.Content.ReadFromJsonAsync<SubmissionResponse>();
        Assert.NotNull(submission);
        Assert.Equal("Stored response", submission.TextAnswer);
        Assert.Equal("Submitted", submission.Status);
    }

    private async Task<Guid> CreatePublishedAssignmentAsync(bool allowSubmissionUpdates)
    {
        using HttpClient adminClient = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        string suffix = Guid.NewGuid().ToString("N").ToUpperInvariant();
        EntityResponse classCourse = await CreateEntityAsync(adminClient, "/api/admin/classes-courses", "CLS-" + suffix, "Class " + suffix[..6]);
        EntityResponse subject = await CreateEntityAsync(adminClient, "/api/admin/subjects", "SUB-" + suffix, "Subject " + suffix[..6]);
        HttpResponseMessage responsibilityResponse = await adminClient.PostAsJsonAsync(
            "/api/admin/teacher-responsibilities",
            new { classCourseId = classCourse.Id, subjectId = subject.Id, teacherInstitutionalId = "TCH-001" });
        responsibilityResponse.EnsureSuccessStatusCode();
        HttpResponseMessage enrollmentResponse = await adminClient.PostAsJsonAsync(
            "/api/admin/enrollments",
            new { classCourseId = classCourse.Id, studentInstitutionalId = "STU-001" });
        enrollmentResponse.EnsureSuccessStatusCode();

        using HttpClient teacherClient = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");
        HttpResponseMessage createResponse = await teacherClient.PostAsJsonAsync(
            "/api/teacher/assignments",
            new
            {
                classCourseId = classCourse.Id,
                subjectId = subject.Id,
                title = "Submission work " + suffix,
                description = "Provide a response before the deadline.",
                deadline = DateTimeOffset.UtcNow.AddDays(7),
                maximumMarks = 20m,
                allowSubmissionUpdates
            });
        createResponse.EnsureSuccessStatusCode();
        EntityResponse assignment = await createResponse.Content.ReadFromJsonAsync<EntityResponse>()
            ?? throw new InvalidOperationException("The assignment response was empty.");
        HttpResponseMessage publishResponse = await teacherClient.PostAsync(
            "/api/teacher/assignments/" + assignment.Id + "/publish",
            null);
        publishResponse.EnsureSuccessStatusCode();

        return assignment.Id;
    }

    private static MultipartFormDataContent CreateSubmissionContent(string? textAnswer)
    {
        MultipartFormDataContent content = new();

        if (textAnswer is not null)
        {
            content.Add(new StringContent(textAnswer), "textAnswer");
        }

        return content;
    }

    private static async Task<EntityResponse> CreateEntityAsync(HttpClient client, string path, string code, string name)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(path, new { code, name });
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<EntityResponse>()
            ?? throw new InvalidOperationException("The academic entity response was empty.");
    }

    private async Task MoveDeadlineToPastAsync(Guid assignmentId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await databaseContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Assignments\" SET \"Deadline\" = {DateTimeOffset.UtcNow.AddDays(-1)} WHERE \"Id\" = {assignmentId}");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string institutionalId, string password)
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { institutionalId, password });
        loginResponse.EnsureSuccessStatusCode();
        string cookie = loginResponse.Headers.GetValues("Set-Cookie").Single().Split(";")[0];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cookie["access_token=".Length..]);

        return client;
    }

    private sealed class EntityResponse
    {
        public Guid Id { get; init; }
    }

    private sealed class SubmissionResponse
    {
        public Guid Id { get; init; }

        public string? TextAnswer { get; init; }

        public string Status { get; init; } = string.Empty;

        public string? AttachmentFileName { get; init; }
    }
}
