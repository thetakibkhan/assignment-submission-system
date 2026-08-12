using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.Assignments;

public sealed class TeacherAssignmentEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public TeacherAssignmentEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateAndPublish_ShouldSucceed_WhenTeacherHasAnActiveAssignedScope()
    {
        using HttpClient adminClient = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        (Guid classCourseId, Guid subjectId) = await CreateTeacherScopeAsync(adminClient);
        using HttpClient teacherClient = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");

        HttpResponseMessage createResponse = await teacherClient.PostAsJsonAsync(
            "/api/teacher/assignments",
            new
            {
                classCourseId,
                subjectId,
                title = "Argumentative essay",
                description = "Write an evidence-based argument using the assigned prompt.",
                deadline = DateTimeOffset.UtcNow.AddDays(7),
                maximumMarks = 20m,
                allowSubmissionUpdates = true
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        AssignmentResponse? assignment = await createResponse.Content.ReadFromJsonAsync<AssignmentResponse>();
        Assert.NotNull(assignment);
        Assert.Equal("Draft", assignment.Status);

        HttpResponseMessage publishResponse = await teacherClient.PostAsync(
            "/api/teacher/assignments/" + assignment.Id + "/publish",
            null);

        Assert.Equal(HttpStatusCode.NoContent, publishResponse.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldRejectTeacher_WhenScopeIsNotAssigned()
    {
        using HttpClient adminClient = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        Guid classCourseId = await CreateClassCourseAsync(adminClient);
        Guid subjectId = await CreateSubjectAsync(adminClient);
        using HttpClient teacherClient = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");

        HttpResponseMessage response = await teacherClient.PostAsJsonAsync(
            "/api/teacher/assignments",
            new { classCourseId, subjectId, title = "Unauthorised draft" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldUpdateDraft_WhenTeacherOwnsTheAssignedScope()
    {
        using HttpClient adminClient = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        (Guid classCourseId, Guid subjectId) = await CreateTeacherScopeAsync(adminClient);
        using HttpClient teacherClient = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");

        HttpResponseMessage createResponse = await teacherClient.PostAsJsonAsync(
            "/api/teacher/assignments",
            new { classCourseId, subjectId, title = "Initial draft" });
        AssignmentResponse? assignment = await createResponse.Content.ReadFromJsonAsync<AssignmentResponse>();

        Assert.NotNull(assignment);

        HttpResponseMessage updateResponse = await teacherClient.PutAsJsonAsync(
            "/api/teacher/assignments/" + assignment.Id,
            new
            {
                classCourseId,
                subjectId,
                title = "Updated essay brief",
                description = "Use evidence from the assigned reading.",
                deadline = DateTimeOffset.UtcNow.AddDays(7),
                maximumMarks = 20m,
                allowSubmissionUpdates = false
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        AssignmentResponse? updatedAssignment = await updateResponse.Content.ReadFromJsonAsync<AssignmentResponse>();

        Assert.Equal("Updated essay brief", updatedAssignment?.Title);
    }

    private async Task<(Guid ClassCourseId, Guid SubjectId)> CreateTeacherScopeAsync(HttpClient adminClient)
    {
        Guid classCourseId = await CreateClassCourseAsync(adminClient);
        Guid subjectId = await CreateSubjectAsync(adminClient);
        HttpResponseMessage response = await adminClient.PostAsJsonAsync(
            "/api/admin/teacher-responsibilities",
            new { classCourseId, subjectId, teacherInstitutionalId = "TCH-001" });
        response.EnsureSuccessStatusCode();
        return (classCourseId, subjectId);
    }

    private static async Task<Guid> CreateClassCourseAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/classes-courses",
            new { code = "CLS-" + Guid.NewGuid().ToString("N").ToUpperInvariant(), name = "Class Nine" });
        response.EnsureSuccessStatusCode();
        EntityResponse? entity = await response.Content.ReadFromJsonAsync<EntityResponse>();
        return entity?.Id ?? throw new InvalidOperationException("The Class/Course response was empty.");
    }

    private static async Task<Guid> CreateSubjectAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/subjects",
            new { code = "SUB-" + Guid.NewGuid().ToString("N").ToUpperInvariant(), name = "English" });
        response.EnsureSuccessStatusCode();
        EntityResponse? entity = await response.Content.ReadFromJsonAsync<EntityResponse>();
        return entity?.Id ?? throw new InvalidOperationException("The Subject response was empty.");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string institutionalId, string password)
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { institutionalId, password });
        loginResponse.EnsureSuccessStatusCode();
        string cookie = loginResponse.Headers.GetValues("Set-Cookie").Single().Split(";")[0];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cookie["access_token=".Length..]);
        return client;
    }

    private sealed class EntityResponse { public Guid Id { get; init; } }
    private sealed class AssignmentResponse
    {
        public Guid Id { get; init; }

        public string Status { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;
    }
}
