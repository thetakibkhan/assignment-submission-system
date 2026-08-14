using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.AcademicSetup;

public sealed class AcademicClassEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public AcademicClassEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ShouldCreateAcademicClass_WhenRequestedByAdmin()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");

        string code = "CLS-" + Guid.NewGuid().ToString("N").ToUpperInvariant();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/classes",
            new
            {
                code,
                name = "Class Nine"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        AcademicClassResponse? academicClass = await response.Content.ReadFromJsonAsync<AcademicClassResponse>();

        Assert.NotNull(academicClass);
        Assert.Equal(code, academicClass.Code);
        Assert.Equal("Class Nine", academicClass.Name);
        Assert.False(academicClass.IsArchived);
    }

    [Fact]
    public async Task Create_ShouldRejectTeacher()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/classes",
            new
            {
                code = "CLS-10",
                name = "Class Ten"
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }


    [Fact]
    public async Task GetAll_ShouldIncludeCreatedAcademicClass_WhenRequestedByAdmin()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        AcademicClassResponse createdAcademicClass = await CreateAcademicClassAsync(client);

        HttpResponseMessage response = await client.GetAsync("/api/admin/classes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        List<AcademicClassResponse>? academicClasses = await response.Content.ReadFromJsonAsync<List<AcademicClassResponse>>();

        Assert.NotNull(academicClasses);
        Assert.Contains(academicClasses, academicClass => academicClass.Id == createdAcademicClass.Id);
    }


    [Fact]
    public async Task GetAll_ShouldIncludeMockAcademicData_WhenRequestedByAdmin()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");

        List<AcademicClassResponse>? academicClasses = await client.GetFromJsonAsync<List<AcademicClassResponse>>(
            "/api/admin/classes");
        List<AcademicClassResponse>? subjects = await client.GetFromJsonAsync<List<AcademicClassResponse>>(
            "/api/admin/subjects");

        Assert.NotNull(academicClasses);
        Assert.NotNull(subjects);
        Assert.Contains(academicClasses, academicClass => academicClass.Code == "CLS-09" && academicClass.Name == "Class Nine");
        Assert.Contains(academicClasses, academicClass => academicClass.Code == "CLS-10" && academicClass.Name == "Class Ten");
        Assert.Contains(subjects, subject => subject.Code == "SUB-MAT" && subject.Name == "Mathematics");
        Assert.Contains(subjects, subject => subject.Code == "SUB-ENG" && subject.Name == "English");
        Assert.Contains(subjects, subject => subject.Code == "SUB-SCI" && subject.Name == "Science");
    }

    [Fact]
    public async Task UpdateAndArchive_ShouldPreserveAcademicClassIdentity_WhenRequestedByAdmin()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        AcademicClassResponse academicClass = await CreateAcademicClassAsync(client);

        HttpResponseMessage updateResponse = await client.PutAsJsonAsync(
            "/api/admin/classes/" + academicClass.Id,
            new
            {
                code = academicClass.Code,
                name = "Updated Class Name"
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        AcademicClassResponse? updatedAcademicClass = await updateResponse.Content.ReadFromJsonAsync<AcademicClassResponse>();

        Assert.NotNull(updatedAcademicClass);
        Assert.Equal(academicClass.Id, updatedAcademicClass.Id);
        Assert.Equal("Updated Class Name", updatedAcademicClass.Name);

        HttpResponseMessage archiveResponse = await client.PostAsync(
            "/api/admin/classes/" + academicClass.Id + "/archive",
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


    private static async Task<AcademicClassResponse> CreateAcademicClassAsync(HttpClient client)
    {
        string code = "CLS-" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/classes",
            new
            {
                code,
                name = "Class Nine"
            });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AcademicClassResponse>()
            ?? throw new InvalidOperationException("The Class response was empty.");
    }
    private sealed class AcademicClassResponse
    {
        public string Code { get; init; } = string.Empty;

        public Guid Id { get; init; }

        public bool IsArchived { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
