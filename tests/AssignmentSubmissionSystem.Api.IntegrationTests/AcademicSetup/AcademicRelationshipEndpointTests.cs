using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.AcademicSetup;

public sealed class AcademicRelationshipEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public AcademicRelationshipEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EnrollAndEnd_ShouldPreserveEnrollment_WhenRequestedByAdmin()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        Guid classCourseId = await CreateClassCourseAsync(client);

        HttpResponseMessage enrollmentResponse = await client.PostAsJsonAsync(
            "/api/admin/enrollments",
            new
            {
                classCourseId,
                studentInstitutionalId = "STU-001"
            });

        Assert.Equal(HttpStatusCode.Created, enrollmentResponse.StatusCode);

        RelationshipResponse? enrollment = await enrollmentResponse.Content.ReadFromJsonAsync<RelationshipResponse>();

        Assert.NotNull(enrollment);
        Assert.True(enrollment.IsActive);

        HttpResponseMessage endResponse = await client.PostAsync(
            "/api/admin/enrollments/" + enrollment.Id + "/end",
            null);

        Assert.Equal(HttpStatusCode.NoContent, endResponse.StatusCode);
    }

    [Fact]
    public async Task AssignAndRevoke_ShouldPreserveTeacherResponsibility_WhenRequestedByAdmin()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        Guid classCourseId = await CreateClassCourseAsync(client);
        Guid subjectId = await CreateSubjectAsync(client);

        HttpResponseMessage assignmentResponse = await client.PostAsJsonAsync(
            "/api/admin/teacher-responsibilities",
            new
            {
                classCourseId,
                subjectId,
                teacherInstitutionalId = "TCH-001"
            });

        Assert.Equal(HttpStatusCode.Created, assignmentResponse.StatusCode);

        RelationshipResponse? responsibility = await assignmentResponse.Content.ReadFromJsonAsync<RelationshipResponse>();

        Assert.NotNull(responsibility);
        Assert.True(responsibility.IsActive);

        HttpResponseMessage revokeResponse = await client.PostAsync(
            "/api/admin/teacher-responsibilities/" + responsibility.Id + "/revoke",
            null);

        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
    }

    [Fact]
    public async Task Assign_ShouldRejectASecondActiveTeacherForTheSameClassCourseAndSubject()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        Guid classCourseId = await CreateClassCourseAsync(client);
        Guid subjectId = await CreateSubjectAsync(client);
        object request = new
        {
            classCourseId,
            subjectId,
            teacherInstitutionalId = "TCH-001"
        };

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync(
            "/api/admin/teacher-responsibilities",
            request);
        firstResponse.EnsureSuccessStatusCode();

        HttpResponseMessage duplicateResponse = await client.PostAsJsonAsync(
            "/api/admin/teacher-responsibilities",
            request);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string institutionalId, string password)
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                institutionalId,
                password
            });

        loginResponse.EnsureSuccessStatusCode();
        string cookie = loginResponse.Headers.GetValues("Set-Cookie").Single().Split(";")[0];
        string accessToken = cookie["access_token=".Length..];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }

    private static async Task<Guid> CreateClassCourseAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/classes-courses",
            new
            {
                code = "CLS-" + Guid.NewGuid().ToString("N").ToUpperInvariant(),
                name = "Class Nine"
            });

        response.EnsureSuccessStatusCode();

        RelationshipResponse? classCourse = await response.Content.ReadFromJsonAsync<RelationshipResponse>();

        return classCourse?.Id ?? throw new InvalidOperationException("The Class/Course response was empty.");
    }

    private static async Task<Guid> CreateSubjectAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/subjects",
            new
            {
                code = "SUB-" + Guid.NewGuid().ToString("N").ToUpperInvariant(),
                name = "Mathematics"
            });

        response.EnsureSuccessStatusCode();

        RelationshipResponse? subject = await response.Content.ReadFromJsonAsync<RelationshipResponse>();

        return subject?.Id ?? throw new InvalidOperationException("The Subject response was empty.");
    }

    private sealed class RelationshipResponse
    {
        public Guid Id { get; init; }

        public bool IsActive { get; init; }
    }
}
