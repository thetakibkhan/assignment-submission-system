using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.Assignments;

public sealed class StudentAssignmentEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public StudentAssignmentEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ShouldReturnOnlyPublishedAssignmentsFromActiveEnrollments()
    {
        using HttpClient adminClient = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        AcademicScope eligibleScope = await CreateAcademicScopeAsync(adminClient, enrollStudent: true);
        AcademicScope unrelatedScope = await CreateAcademicScopeAsync(adminClient, enrollStudent: false);
        using HttpClient teacherClient = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");
        Guid eligibleAssignmentId = await CreateAssignmentAsync(teacherClient, eligibleScope, "Eligible published work", publish: true);
        await CreateAssignmentAsync(teacherClient, eligibleScope, "Hidden draft", publish: false);
        await CreateAssignmentAsync(teacherClient, unrelatedScope, "Other class work", publish: true);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");

        HttpResponseMessage response = await studentClient.GetAsync("/api/student/assignments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        IReadOnlyList<StudentAssignmentResponse>? assignments = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<StudentAssignmentResponse>>();

        Assert.NotNull(assignments);
        StudentAssignmentResponse assignment = Assert.Single(
            assignments,
            item => item.Id == eligibleAssignmentId);
        Assert.Equal(eligibleAssignmentId, assignment.Id);
        Assert.Equal(eligibleScope.ClassCourseName, assignment.ClassCourseName);
        Assert.Equal(eligibleScope.SubjectName, assignment.SubjectName);
        Assert.Equal("Not submitted", assignment.StudentState);
        Assert.Null(assignment.Submission);
        Assert.DoesNotContain(assignments, item => item.Title == "Hidden draft");
        Assert.DoesNotContain(assignments, item => item.Title == "Other class work");
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenAssignmentIsDraftOrOutsideEnrollment()
    {
        using HttpClient adminClient = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        AcademicScope eligibleScope = await CreateAcademicScopeAsync(adminClient, enrollStudent: true);
        AcademicScope unrelatedScope = await CreateAcademicScopeAsync(adminClient, enrollStudent: false);
        using HttpClient teacherClient = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");
        Guid draftId = await CreateAssignmentAsync(teacherClient, eligibleScope, "Private draft", publish: false);
        Guid unrelatedId = await CreateAssignmentAsync(teacherClient, unrelatedScope, "Unrelated work", publish: true);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");

        HttpResponseMessage draftResponse = await studentClient.GetAsync("/api/student/assignments/" + draftId);
        HttpResponseMessage unrelatedResponse = await studentClient.GetAsync("/api/student/assignments/" + unrelatedId);

        Assert.Equal(HttpStatusCode.NotFound, draftResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unrelatedResponse.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldOrderOpenBeforePastAssignments()
    {
        using HttpClient adminClient = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        AcademicScope scope = await CreateAcademicScopeAsync(adminClient, enrollStudent: true);
        using HttpClient teacherClient = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");
        Guid pastAssignmentId = await CreateAssignmentAsync(teacherClient, scope, "Past assignment", publish: true);
        Guid openAssignmentId = await CreateAssignmentAsync(teacherClient, scope, "Open assignment", publish: true);
        await MoveDeadlineToPastAsync(pastAssignmentId);
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");

        IReadOnlyList<StudentAssignmentResponse>? assignments = await studentClient
            .GetFromJsonAsync<IReadOnlyList<StudentAssignmentResponse>>("/api/student/assignments");

        Assert.NotNull(assignments);
        int openIndex = assignments.ToList().FindIndex(item => item.Id == openAssignmentId);
        int pastIndex = assignments.ToList().FindIndex(item => item.Id == pastAssignmentId);

        Assert.True(openIndex >= 0);
        Assert.True(pastIndex > openIndex);
        Assert.False(assignments[openIndex].DeadlinePassed);
        StudentAssignmentResponse pastAssignment = Assert.Single(
            assignments,
            item => item.Id == pastAssignmentId);
        Assert.True(pastAssignment.DeadlinePassed);
    }

    private async Task<AcademicScope> CreateAcademicScopeAsync(HttpClient adminClient, bool enrollStudent)
    {
        string suffix = Guid.NewGuid().ToString("N").ToUpperInvariant();
        EntityResponse classCourse = await CreateEntityAsync(
            adminClient,
            "/api/admin/classes-courses",
            "CLS-" + suffix,
            "Class " + suffix[..6]);
        EntityResponse subject = await CreateEntityAsync(
            adminClient,
            "/api/admin/subjects",
            "SUB-" + suffix,
            "Subject " + suffix[..6]);

        HttpResponseMessage responsibilityResponse = await adminClient.PostAsJsonAsync(
            "/api/admin/teacher-responsibilities",
            new
            {
                classCourseId = classCourse.Id,
                subjectId = subject.Id,
                teacherInstitutionalId = "TCH-001"
            });
        responsibilityResponse.EnsureSuccessStatusCode();

        if (enrollStudent)
        {
            HttpResponseMessage enrollmentResponse = await adminClient.PostAsJsonAsync(
                "/api/admin/enrollments",
                new
                {
                    classCourseId = classCourse.Id,
                    studentInstitutionalId = "STU-001"
                });
            enrollmentResponse.EnsureSuccessStatusCode();
        }

        return new AcademicScope(
            classCourse.Id,
            classCourse.Name,
            subject.Id,
            subject.Name);
    }

    private static async Task<EntityResponse> CreateEntityAsync(
        HttpClient client,
        string path,
        string code,
        string name)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(path, new { code, name });
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<EntityResponse>()
            ?? throw new InvalidOperationException("The academic entity response was empty.");
    }

    private static async Task<Guid> CreateAssignmentAsync(
        HttpClient teacherClient,
        AcademicScope scope,
        string title,
        bool publish)
    {
        HttpResponseMessage createResponse = await teacherClient.PostAsJsonAsync(
            "/api/teacher/assignments",
            new
            {
                classCourseId = scope.ClassCourseId,
                subjectId = scope.SubjectId,
                title,
                description = "Complete the work described in this assignment.",
                deadline = DateTimeOffset.UtcNow.AddDays(7),
                maximumMarks = 20m,
                allowSubmissionUpdates = true
            });
        createResponse.EnsureSuccessStatusCode();
        EntityResponse assignment = await createResponse.Content.ReadFromJsonAsync<EntityResponse>()
            ?? throw new InvalidOperationException("The assignment response was empty.");

        if (publish)
        {
            HttpResponseMessage publishResponse = await teacherClient.PostAsync(
                "/api/teacher/assignments/" + assignment.Id + "/publish",
                null);
            publishResponse.EnsureSuccessStatusCode();
        }

        return assignment.Id;
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
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { institutionalId, password });
        loginResponse.EnsureSuccessStatusCode();
        string cookie = loginResponse.Headers.GetValues("Set-Cookie").Single().Split(";")[0];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            cookie["access_token=".Length..]);

        return client;
    }

    private sealed record AcademicScope(
        Guid ClassCourseId,
        string ClassCourseName,
        Guid SubjectId,
        string SubjectName);

    private sealed class EntityResponse
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }

    private sealed class StudentAssignmentResponse
    {
        public Guid Id { get; init; }

        public string ClassCourseName { get; init; } = string.Empty;

        public bool DeadlinePassed { get; init; }

        public string StudentState { get; init; } = string.Empty;

        public string SubjectName { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public object? Submission { get; init; }
    }
}
