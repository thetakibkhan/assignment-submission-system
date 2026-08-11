using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.AcademicSetup;

public sealed class ClassCourseEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public ClassCourseEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ShouldCreateClassCourse_WhenRequestedByAdmin()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");

        string code = "CLS-" + Guid.NewGuid().ToString("N").ToUpperInvariant();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/classes-courses",
            new
            {
                code,
                name = "Class Nine"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        ClassCourseResponse? classCourse = await response.Content.ReadFromJsonAsync<ClassCourseResponse>();

        Assert.NotNull(classCourse);
        Assert.Equal(code, classCourse.Code);
        Assert.Equal("Class Nine", classCourse.Name);
        Assert.False(classCourse.IsArchived);
    }

    [Fact]
    public async Task Create_ShouldRejectTeacher()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/classes-courses",
            new
            {
                code = "CLS-10",
                name = "Class Ten"
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }


    [Fact]
    public async Task GetAll_ShouldIncludeCreatedClassCourse_WhenRequestedByAdmin()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        ClassCourseResponse createdClassCourse = await CreateClassCourseAsync(client);

        HttpResponseMessage response = await client.GetAsync("/api/admin/classes-courses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        List<ClassCourseResponse>? classCourses = await response.Content.ReadFromJsonAsync<List<ClassCourseResponse>>();

        Assert.NotNull(classCourses);
        Assert.Contains(classCourses, classCourse => classCourse.Id == createdClassCourse.Id);
    }


    [Fact]
    public async Task UpdateAndArchive_ShouldPreserveClassCourseIdentity_WhenRequestedByAdmin()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        ClassCourseResponse classCourse = await CreateClassCourseAsync(client);

        HttpResponseMessage updateResponse = await client.PutAsJsonAsync(
            "/api/admin/classes-courses/" + classCourse.Id,
            new
            {
                code = classCourse.Code,
                name = "Updated Class Name"
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        ClassCourseResponse? updatedClassCourse = await updateResponse.Content.ReadFromJsonAsync<ClassCourseResponse>();

        Assert.NotNull(updatedClassCourse);
        Assert.Equal(classCourse.Id, updatedClassCourse.Id);
        Assert.Equal("Updated Class Name", updatedClassCourse.Name);

        HttpResponseMessage archiveResponse = await client.PostAsync(
            "/api/admin/classes-courses/" + classCourse.Id + "/archive",
            null);

        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
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


    private static async Task<ClassCourseResponse> CreateClassCourseAsync(HttpClient client)
    {
        string code = "CLS-" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/classes-courses",
            new
            {
                code,
                name = "Class Nine"
            });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ClassCourseResponse>()
            ?? throw new InvalidOperationException("The Class/Course response was empty.");
    }
    private sealed class ClassCourseResponse
    {
        public string Code { get; init; } = string.Empty;

        public Guid Id { get; init; }

        public bool IsArchived { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
