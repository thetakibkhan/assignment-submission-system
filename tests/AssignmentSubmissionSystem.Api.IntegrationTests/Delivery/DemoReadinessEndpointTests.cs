using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.Delivery;

public sealed class DemoReadinessEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private const string HistoricalResultTitle = "Historical reading response";
    private const string OpenAssignmentTitle = "Argument essay";
    private readonly AuthWebApplicationFactory _factory;

    public DemoReadinessEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DemoSeed_ShouldProvideOpenWorkAndAnAlreadyDisclosedResult_ForTheStudentJourney()
    {
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");

        IReadOnlyList<AssignmentResponse> assignments = await studentClient.GetFromJsonAsync<IReadOnlyList<AssignmentResponse>>("/api/student/assignments")
            ?? throw new InvalidOperationException("The student assignment list was empty.");
        AssignmentResponse openAssignment = Assert.Single(assignments, assignment => assignment.Title == OpenAssignmentTitle);
        AssignmentResponse historicalAssignment = Assert.Single(assignments, assignment => assignment.Title == HistoricalResultTitle);

        Assert.False(openAssignment.DeadlinePassed);
        Assert.True(historicalAssignment.DeadlinePassed);

        SubmissionResponse historicalSubmission = await studentClient.GetFromJsonAsync<SubmissionResponse>("/api/student/assignments/" + historicalAssignment.Id + "/submission")
            ?? throw new InvalidOperationException("The seeded historical submission was not found.");
        Assert.Equal("Graded", historicalSubmission.Status);
        Assert.NotNull(historicalSubmission.Marks);
        Assert.False(string.IsNullOrWhiteSpace(historicalSubmission.Feedback));
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

    private sealed class AssignmentResponse
    {
        public bool DeadlinePassed { get; init; }
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
    }

    private sealed class SubmissionResponse
    {
        public string? Feedback { get; init; }
        public decimal? Marks { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
