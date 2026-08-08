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
}
