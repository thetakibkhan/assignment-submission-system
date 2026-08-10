using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.Authentication;

public sealed class LoginEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LoginEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
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

    private sealed class LoginResponse
    {
        public string RedirectPath { get; init; } = string.Empty;

        public string Role { get; init; } = string.Empty;
    }
}
