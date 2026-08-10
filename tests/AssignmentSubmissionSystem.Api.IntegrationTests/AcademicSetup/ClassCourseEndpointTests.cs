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

        string code = "CLS-" + Guid.CreateVersion7().ToString("N")[..8].ToUpperInvariant();

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

    private sealed class ClassCourseResponse
    {
        public string Code { get; init; } = string.Empty;

        public bool IsArchived { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
