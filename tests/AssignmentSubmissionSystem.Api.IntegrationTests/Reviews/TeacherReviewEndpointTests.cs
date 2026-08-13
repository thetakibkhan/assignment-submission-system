using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.Reviews;

public sealed class TeacherReviewEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public TeacherReviewEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetQueue_ShouldReturnSubmittedWork_ForTheOwningTeacher()
    {
        Guid assignmentId = await CreatePublishedAssignmentAsync(allowSubmissionUpdates: false);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");
        using MultipartFormDataContent submissionContent = CreateSubmissionContent("Queue response");
        HttpResponseMessage createResponse = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            submissionContent);
        createResponse.EnsureSuccessStatusCode();

        using HttpClient teacherClient = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");
        HttpResponseMessage response = await teacherClient.GetAsync(
            "/api/teacher/assignments/" + assignmentId + "/submissions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        IReadOnlyList<TeacherSubmissionResponse>? submissions = await response.Content.ReadFromJsonAsync<IReadOnlyList<TeacherSubmissionResponse>>();
        TeacherSubmissionResponse submission = Assert.Single(submissions ?? []);
        Assert.Equal("Ayesha Rahman", submission.StudentName);
        Assert.Equal("Queue response", submission.TextAnswer);
        Assert.Equal("Submitted", submission.Status);
    }

    [Fact]
    public async Task ReviewLifecycle_ShouldRequireOwnerAndPublishResultsOnlyAfterGrading()
    {
        Guid assignmentId = await CreatePublishedAssignmentAsync(allowSubmissionUpdates: false);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");
        using MultipartFormDataContent submissionContent = CreateSubmissionContent("My reviewed work");
        HttpResponseMessage createResponse = await studentClient.PostAsync(
            "/api/student/assignments/" + assignmentId + "/submission",
            submissionContent);
        createResponse.EnsureSuccessStatusCode();
        SubmissionResponse submission = await createResponse.Content.ReadFromJsonAsync<SubmissionResponse>()
            ?? throw new InvalidOperationException("The submission response was empty.");

        using HttpClient teacherClient = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");
        HttpResponseMessage startResponse = await teacherClient.PostAsync(
            "/api/teacher/submissions/" + submission.Id + "/start-review",
            null);
        Assert.Equal(HttpStatusCode.NoContent, startResponse.StatusCode);

        HttpResponseMessage reviewResponse = await teacherClient.PutAsJsonAsync(
            "/api/teacher/submissions/" + submission.Id + "/review",
            new { marks = 18m, feedback = "Strong analysis." });
        Assert.Equal(HttpStatusCode.NoContent, reviewResponse.StatusCode);

        HttpResponseMessage preGradeStudentResponse = await studentClient.GetAsync(
            "/api/student/assignments/" + assignmentId + "/submission");
        SubmissionResponse preGradeSubmission = await preGradeStudentResponse.Content.ReadFromJsonAsync<SubmissionResponse>()
            ?? throw new InvalidOperationException("The student submission response was empty.");
        Assert.Null(preGradeSubmission.Marks);
        Assert.Null(preGradeSubmission.Feedback);

        HttpResponseMessage gradeResponse = await teacherClient.PostAsync(
            "/api/teacher/submissions/" + submission.Id + "/grade",
            null);
        Assert.Equal(HttpStatusCode.NoContent, gradeResponse.StatusCode);

        HttpResponseMessage gradedStudentResponse = await studentClient.GetAsync(
            "/api/student/assignments/" + assignmentId + "/submission");
        SubmissionResponse gradedSubmission = await gradedStudentResponse.Content.ReadFromJsonAsync<SubmissionResponse>()
            ?? throw new InvalidOperationException("The graded submission response was empty.");
        Assert.Equal("Graded", gradedSubmission.Status);
        Assert.Equal(18m, gradedSubmission.Marks);
        Assert.Equal("Strong analysis.", gradedSubmission.Feedback);
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

    private sealed class TeacherSubmissionResponse
    {
        public string Status { get; init; } = string.Empty;

        public string StudentName { get; init; } = string.Empty;

        public string? TextAnswer { get; init; }
    }

    private sealed class SubmissionResponse
    {
        public string? Feedback { get; init; }

        public Guid Id { get; init; }

        public decimal? Marks { get; init; }

        public string Status { get; init; } = string.Empty;
    }
}
