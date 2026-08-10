using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.Authentication;

public sealed class LoginEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AuthWebApplicationFactory _factory;

    public LoginEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    [Theory]
    [InlineData("admin@assignment.local", "Admin123!", "Admin", "/admin")]
    [InlineData("teacher@assignment.local", "Teacher123!", "Teacher", "/teacher")]
    [InlineData("student@assignment.local", "Student123!", "Student", "/student")]
    public async Task Login_ShouldReturnRoleSpecificDestination_WhenCredentialsAreValid(
        string email,
        string password,
        string expectedRole,
        string expectedRedirectPath)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email,
                password
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        LoginResponse? loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResponse);
        Assert.Equal(expectedRole, loginResponse.Role);
        Assert.Equal(expectedRedirectPath, loginResponse.RedirectPath);
        Assert.Contains("access_token=", response.Headers.GetValues("Set-Cookie").Single());
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "student@example.test",
                password = "IncorrectPassword1"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AccountDeactivation_ShouldImmediatelyBlockLoginAndProtectedAccess()
    {
        HttpResponseMessage anonymousResponse = await _client.GetAsync("/api/dashboard/admin");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        HttpResponseMessage adminLoginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "admin@assignment.local",
                password = "Admin123!"
            });

        Assert.Equal(HttpStatusCode.OK, adminLoginResponse.StatusCode);
        string adminCookie = adminLoginResponse.Headers.GetValues("Set-Cookie").Single().Split(";")[0];
        string accessToken = adminCookie["access_token=".Length..];
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/dashboard/admin")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/dashboard/teacher")).StatusCode);

        using HttpClient studentClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        HttpResponseMessage studentLoginResponse = await studentClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "student@assignment.local",
                password = "Student123!"
            });
        string studentCookie = studentLoginResponse.Headers.GetValues("Set-Cookie").Single().Split(";")[0];
        string studentAccessToken = studentCookie["access_token=".Length..];
        studentClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", studentAccessToken);

        Assert.Equal(HttpStatusCode.OK, studentLoginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await studentClient.GetAsync("/api/dashboard/student")).StatusCode);

        HttpResponseMessage deactivationResponse = await _client.PutAsJsonAsync(
            "/api/admin/users/student@assignment.local/activation",
            new { isActive = false });

        Assert.Equal(HttpStatusCode.NoContent, deactivationResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await studentClient.GetAsync("/api/dashboard/student")).StatusCode);

        HttpResponseMessage inactiveLoginResponse = await studentClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "student@assignment.local",
                password = "Student123!"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, inactiveLoginResponse.StatusCode);

        HttpResponseMessage reactivationResponse = await _client.PutAsJsonAsync(
            "/api/admin/users/student@assignment.local/activation",
            new { isActive = true });

        Assert.Equal(HttpStatusCode.NoContent, reactivationResponse.StatusCode);
    }

    private sealed class LoginResponse
    {
        public string RedirectPath { get; init; } = string.Empty;

        public string Role { get; init; } = string.Empty;
    }
}
