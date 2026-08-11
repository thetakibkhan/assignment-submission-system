using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;
using AssignmentSubmissionSystem.Domain.Accounts;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.Administration;

public sealed class AccountManagementEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public AccountManagementEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateAndChangePassword_ShouldRequireANewManagedUserToChangeTheTemporaryPassword()
    {
        using HttpClient adminClient = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        string institutionalId = "STU-" + Guid.NewGuid().ToString("N").ToUpperInvariant();

        HttpResponseMessage createResponse = await adminClient.PostAsJsonAsync(
            "/api/admin/users",
            new
            {
                email = institutionalId.ToLowerInvariant() + "@example.test",
                fullName = "New Student",
                institutionalId,
                role = "Student"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreatedAccountResponse? createdAccount = await createResponse.Content.ReadFromJsonAsync<CreatedAccountResponse>();

        Assert.NotNull(createdAccount);
        Assert.Equal(institutionalId, createdAccount.InstitutionalId);
        Assert.Equal("Student", createdAccount.Role);
        Assert.False(string.IsNullOrWhiteSpace(createdAccount.TemporaryPassword));

        using HttpClient newUserClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        HttpResponseMessage temporaryLoginResponse = await newUserClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                institutionalId,
                password = createdAccount.TemporaryPassword
            });

        Assert.Equal(HttpStatusCode.OK, temporaryLoginResponse.StatusCode);

        LoginResponse? temporaryLogin = await temporaryLoginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(temporaryLogin);
        Assert.True(temporaryLogin.RequiresPasswordChange);
        Assert.Equal("/change-password", temporaryLogin.RedirectPath);

        AddBearerToken(newUserClient, temporaryLoginResponse);
        string newPassword = "ChangedPassword1!";
        HttpResponseMessage changePasswordResponse = await newUserClient.PostAsJsonAsync(
            "/api/auth/change-password",
            new
            {
                currentPassword = createdAccount.TemporaryPassword,
                newPassword
            });

        Assert.Equal(HttpStatusCode.OK, changePasswordResponse.StatusCode);

        using HttpClient verifiedClient = _factory.CreateClient();
        HttpResponseMessage verifiedLoginResponse = await verifiedClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                institutionalId,
                password = newPassword
            });

        Assert.Equal(HttpStatusCode.OK, verifiedLoginResponse.StatusCode);

        LoginResponse? verifiedLogin = await verifiedLoginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(verifiedLogin);
        Assert.False(verifiedLogin.RequiresPasswordChange);
    }

    [Fact]
    public async Task ManageUser_ShouldUpdateProfileActivationAndResetPassword_WhenRequestedByAdmin()
    {
        using HttpClient adminClient = await CreateAuthenticatedClientAsync("ADM-001", "Admin123!");
        string originalInstitutionalId = "TCH-" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        CreatedAccountResponse createdAccount = await CreateManagedAccountAsync(
            adminClient,
            originalInstitutionalId,
            "Teacher");
        string correctedInstitutionalId = originalInstitutionalId + "-CORRECTED";

        HttpResponseMessage updateResponse = await adminClient.PutAsJsonAsync(
            "/api/admin/users/" + originalInstitutionalId,
            new
            {
                email = correctedInstitutionalId.ToLowerInvariant() + "@example.test",
                fullName = "Corrected Teacher Name",
                institutionalId = correctedInstitutionalId
            });

        Assert.True(
            updateResponse.StatusCode == HttpStatusCode.OK,
            await updateResponse.Content.ReadAsStringAsync());

        HttpResponseMessage deactivateResponse = await adminClient.PutAsJsonAsync(
            "/api/admin/users/" + correctedInstitutionalId + "/activation",
            new
            {
                isActive = false
            });

        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        HttpResponseMessage resetResponse = await adminClient.PostAsync(
            "/api/admin/users/" + correctedInstitutionalId + "/reset-password",
            null);

        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        ResetPasswordResponse? resetPassword = await resetResponse.Content.ReadFromJsonAsync<ResetPasswordResponse>();

        Assert.NotNull(resetPassword);
        Assert.False(string.IsNullOrWhiteSpace(resetPassword.TemporaryPassword));

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Guid targetUserId = await databaseContext.Users
            .Where(user => user.UserName == correctedInstitutionalId)
            .Select(user => user.Id)
            .SingleAsync();
        List<AccountAuditEventType> auditEventTypes = await databaseContext.AccountAuditEvents
            .Where(auditEvent => auditEvent.TargetUserId == targetUserId)
            .Select(auditEvent => auditEvent.EventType)
            .ToListAsync();

        Assert.Contains(AccountAuditEventType.AccountCreated, auditEventTypes);
        Assert.Contains(AccountAuditEventType.ProfileUpdated, auditEventTypes);
        Assert.Contains(AccountAuditEventType.ActivationChanged, auditEventTypes);
        Assert.Contains(AccountAuditEventType.PasswordReset, auditEventTypes);
    }

    [Fact]
    public async Task Create_ShouldRejectTeacher()
    {
        using HttpClient teacherClient = await CreateAuthenticatedClientAsync("TCH-001", "Teacher123!");

        HttpResponseMessage response = await teacherClient.PostAsJsonAsync(
            "/api/admin/users",
            new
            {
                fullName = "Unauthorized Student",
                institutionalId = "STU-UNAUTHORIZED",
                role = "Student"
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<CreatedAccountResponse> CreateManagedAccountAsync(
        HttpClient adminClient,
        string institutionalId,
        string role)
    {
        HttpResponseMessage response = await adminClient.PostAsJsonAsync(
            "/api/admin/users",
            new
            {
                email = institutionalId.ToLowerInvariant() + "@example.test",
                fullName = "Managed User",
                institutionalId,
                role
            });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CreatedAccountResponse>()
            ?? throw new InvalidOperationException("The managed-account response was empty.");
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
        AddBearerToken(client, loginResponse);

        return client;
    }

    private static void AddBearerToken(HttpClient client, HttpResponseMessage loginResponse)
    {
        string cookie = loginResponse.Headers.GetValues("Set-Cookie").Single().Split(";")[0];
        string accessToken = cookie["access_token=".Length..];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private sealed class CreatedAccountResponse
    {
        public string InstitutionalId { get; init; } = string.Empty;

        public string Role { get; init; } = string.Empty;

        public string TemporaryPassword { get; init; } = string.Empty;
    }

    private sealed class LoginResponse
    {
        public bool RequiresPasswordChange { get; init; }

        public string RedirectPath { get; init; } = string.Empty;
    }

    private sealed class ResetPasswordResponse
    {
        public string TemporaryPassword { get; init; } = string.Empty;
    }
}
