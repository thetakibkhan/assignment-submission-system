using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssignmentSubmissionSystem.Api.IntegrationTests.Infrastructure;

public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    public AuthWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Host=auth-test-db;Port=5432;Database=assignment_submission_tests;Username=postgres;Password=TestOnlyPostgresPassword123!");
        Environment.SetEnvironmentVariable("DemoAccounts__AdminPassword", "Admin123!");
        Environment.SetEnvironmentVariable("DemoAccounts__TeacherPassword", "Teacher123!");
        Environment.SetEnvironmentVariable("DemoAccounts__StudentPassword", "Student123!");
        Environment.SetEnvironmentVariable("DemoData__Enabled", "true");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenLifetimeMinutes", "15");
        Environment.SetEnvironmentVariable("Jwt__Audience", "assignment-submission-system-tests");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "assignment-submission-system-tests");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "ThisIsOnlyAnIntegrationTestSigningKey123!");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
