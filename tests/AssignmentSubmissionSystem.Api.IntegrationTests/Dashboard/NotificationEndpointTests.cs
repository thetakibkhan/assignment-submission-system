using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.Dashboard;

public sealed class NotificationEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public NotificationEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetNotifications_ShouldReturnOnlyTheCurrentUsersNotifications()
    {
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");

        HttpResponseMessage response = await studentClient.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        IReadOnlyList<NotificationResponse>? notifications = await response.Content.ReadFromJsonAsync<IReadOnlyList<NotificationResponse>>();
        Assert.NotNull(notifications);
        Assert.All(notifications, notification => Assert.False(string.IsNullOrWhiteSpace(notification.Message)));
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

    private sealed class NotificationResponse
    {
        public string Message { get; init; } = string.Empty;
    }
}
