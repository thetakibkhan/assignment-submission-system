using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;
using AssignmentSubmissionSystem.Domain.Notifications;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task Delete_ShouldRemoveNotification_WhenItBelongsToCurrentUser()
    {
        Guid notificationId = await CreateNotificationAsync("STU-001");
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");

        HttpResponseMessage deleteResponse = await studentClient.DeleteAsync(
            "/api/notifications/" + notificationId);
        IReadOnlyList<NotificationResponse>? remainingNotifications = await studentClient
            .GetFromJsonAsync<IReadOnlyList<NotificationResponse>>("/api/notifications");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.DoesNotContain(
            remainingNotifications ?? [],
            notification => notification.Id == notificationId);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenNotificationBelongsToAnotherUser()
    {
        Guid notificationId = await CreateNotificationAsync("TCH-001");
        using HttpClient studentClient = await CreateAuthenticatedClientAsync("STU-001", "Student123!");
        using HttpClient teacherClient = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");

        HttpResponseMessage deleteResponse = await studentClient.DeleteAsync(
            "/api/notifications/" + notificationId);
        IReadOnlyList<NotificationResponse>? teacherNotifications = await teacherClient
            .GetFromJsonAsync<IReadOnlyList<NotificationResponse>>("/api/notifications");

        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
        Assert.Contains(
            teacherNotifications ?? [],
            notification => notification.Id == notificationId);
    }

    private async Task<Guid> CreateNotificationAsync(string institutionalId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Guid recipientUserId = await databaseContext.Users
            .Where(user => user.UserName == institutionalId)
            .Select(user => user.Id)
            .SingleAsync();
        Guid assignmentId = await databaseContext.Assignments
            .Select(assignment => assignment.Id)
            .FirstAsync();
        UserNotification notification = new(
            Guid.CreateVersion7(),
            recipientUserId,
            NotificationType.AssignmentPublished,
            assignmentId,
            null,
            "Deletion test notification.",
            DateTimeOffset.UtcNow);

        await databaseContext.UserNotifications.AddAsync(notification);
        await databaseContext.SaveChangesAsync();

        return notification.Id;
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
        public Guid Id { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
