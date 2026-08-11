using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.AcademicSetup;

public sealed class SubjectEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public SubjectEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ShouldCreateSubject_WhenRequestedByAdmin()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        string code = CreateSubjectCode();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/subjects",
            new
            {
                code,
                name = "Mathematics"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        SubjectResponse? subject = await response.Content.ReadFromJsonAsync<SubjectResponse>();

        Assert.NotNull(subject);
        Assert.Equal(code, subject.Code);
        Assert.Equal("Mathematics", subject.Name);
        Assert.False(subject.IsArchived);
    }

    [Fact]
    public async Task Create_ShouldRejectTeacher()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/subjects",
            new
            {
                code = CreateSubjectCode(),
                name = "Mathematics"
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldIncludeCreatedSubject_WhenRequestedByAdmin()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        SubjectResponse createdSubject = await CreateSubjectAsync(client);

        HttpResponseMessage response = await client.GetAsync("/api/admin/subjects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        List<SubjectResponse>? subjects = await response.Content.ReadFromJsonAsync<List<SubjectResponse>>();

        Assert.NotNull(subjects);
        Assert.Contains(subjects, subject => subject.Id == createdSubject.Id);
    }


    [Fact]
    public async Task UpdateAndArchive_ShouldPreserveSubjectIdentity_WhenRequestedByAdmin()
    {
        using HttpClient client = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        SubjectResponse subject = await CreateSubjectAsync(client);

        HttpResponseMessage updateResponse = await client.PutAsJsonAsync(
            "/api/admin/subjects/" + subject.Id,
            new
            {
                code = subject.Code,
                name = "Advanced Mathematics"
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        SubjectResponse? updatedSubject = await updateResponse.Content.ReadFromJsonAsync<SubjectResponse>();

        Assert.NotNull(updatedSubject);
        Assert.Equal(subject.Id, updatedSubject.Id);
        Assert.Equal("Advanced Mathematics", updatedSubject.Name);

        HttpResponseMessage archiveResponse = await client.PostAsync(
            "/api/admin/subjects/" + subject.Id + "/archive",
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

    private static string CreateSubjectCode()
    {
        return "SUB-" + Guid.NewGuid().ToString("N").ToUpperInvariant();
    }

    private static async Task<SubjectResponse> CreateSubjectAsync(HttpClient client)
    {
        string code = CreateSubjectCode();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/subjects",
            new
            {
                code,
                name = "Mathematics"
            });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SubjectResponse>()
            ?? throw new InvalidOperationException("The Subject response was empty.");
    }

    private sealed class SubjectResponse
    {
        public string Code { get; init; } = string.Empty;

        public Guid Id { get; init; }

        public bool IsArchived { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
