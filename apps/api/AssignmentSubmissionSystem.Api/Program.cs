using System.Security.Claims;
using System.Threading.RateLimiting;
using AssignmentSubmissionSystem.Api.Authentication;
using System.Text;
using AssignmentSubmissionSystem.Application.AccountManagement;
using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Application.Submissions;
using AssignmentSubmissionSystem.Application.Notifications;
using AssignmentSubmissionSystem.Application.Dashboards;
using AssignmentSubmissionSystem.Application.AcademicSetup;
using AssignmentSubmissionSystem.Application.AcademicSetup.AcademicClasses;
using AssignmentSubmissionSystem.Application.AcademicSetup.Enrollments;
using AssignmentSubmissionSystem.Application.AcademicSetup.Subjects;
using AssignmentSubmissionSystem.Application.AcademicSetup.TeacherResponsibilities;
using AssignmentSubmissionSystem.Infrastructure.AccountManagement;
using AssignmentSubmissionSystem.Infrastructure.Assignments;
using AssignmentSubmissionSystem.Infrastructure.AcademicSetup;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;
using AssignmentSubmissionSystem.Infrastructure.Submissions;
using AssignmentSubmissionSystem.Infrastructure.Notifications;
using AssignmentSubmissionSystem.Infrastructure.Dashboards;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

JwtOptions jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration is required.");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer)
    || string.IsNullOrWhiteSpace(jwtOptions.Audience)
    || string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
    || jwtOptions.SigningKey.Length < 32)
{
    throw new InvalidOperationException("JWT configuration must include issuer, audience, and a signing key of at least 32 characters.");
}

string connectionString = GetConnectionString(builder.Configuration);

string? dataProtectionKeyRingPath = builder.Configuration["DataProtection:KeyRingPath"];

if (!string.IsNullOrWhiteSpace(dataProtectionKeyRingPath))
{
    Directory.CreateDirectory(dataProtectionKeyRingPath);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyRingPath));
}

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Assignment Submission System API",
        Version = "v1",
        Description = "Local evaluator API. Sign in through POST /api/auth/login; the browser receives an HTTP-only cookie and each endpoint enforces its documented role."
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("Authentication", httpContext =>
    {
        string clientAddress = GetRateLimitClientAddress(httpContext);

        return RateLimitPartition.GetFixedWindowLimiter(clientAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    // Institutional ID is the login identity. Contact email is optional and may be shared.
    options.User.RequireUniqueEmail = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.Configure<DemoAccountOptions>(builder.Configuration.GetSection(DemoAccountOptions.SectionName));
builder.Services.Configure<DemoDataOptions>(builder.Configuration.GetSection(DemoDataOptions.SectionName));
builder.Services.Configure<BootstrapAdminOptions>(
    builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SubmissionStorageOptions>(
    builder.Configuration.GetSection(SubmissionStorageOptions.SectionName));
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<DemoScenarioSeeder>();
builder.Services.AddScoped<IAccountManagementService, AccountManagementService>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IStudentAssignmentQuery, StudentAssignmentQuery>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<ISubmissionFileStorage, LocalSubmissionFileStorage>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardQuery, DashboardQuery>();
builder.Services.AddScoped<IAcademicClassRepository, AcademicClassRepository>();
builder.Services.AddScoped<IAcademicClassService, AcademicClassService>();
builder.Services.AddScoped<IAcademicUserDirectory, AcademicUserDirectory>();
builder.Services.AddScoped<IStudentEnrollmentRepository, StudentEnrollmentRepository>();
builder.Services.AddScoped<IStudentEnrollmentService, StudentEnrollmentService>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ITeacherResponsibilityRepository, TeacherResponsibilityRepository>();
builder.Services.AddScoped<ITeacherResponsibilityService, TeacherResponsibilityService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ClockSkew = TimeSpan.FromMinutes(1),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            ValidAudience = jwtOptions.Audience,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("access_token", out string? accessToken))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                string? userId = context.Principal?
                    .FindFirst(ClaimTypes.NameIdentifier)?
                    .Value;

                if (!Guid.TryParse(userId, out Guid parsedUserId))
                {
                    context.Fail("The access token subject is invalid.");
                    return;
                }

                UserManager<ApplicationUser> userManager = context.HttpContext.RequestServices
                    .GetRequiredService<UserManager<ApplicationUser>>();
                ApplicationUser? user = await userManager.FindByIdAsync(parsedUserId.ToString());

                if (user is null || !user.IsActive)
                {
                    context.Fail("The user account is inactive.");
                    return;
                }

                if (user.MustChangePassword && context.Principal?.Identity is ClaimsIdentity identity)
                {
                    identity.AddClaim(new Claim(AuthorizationPolicies.PasswordChangeRequiredClaim, "true"));
                }
            },
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.NormalAccess, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => !context.User.HasClaim(AuthorizationPolicies.PasswordChangeRequiredClaim, "true"));
    });
});
string[] allowedWebOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebApplication", policy =>
    {
        policy.WithOrigins(allowedWebOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("WebApplication");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

using (IServiceScope scope = app.Services.CreateScope())
{
    DatabaseInitializer databaseInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await databaseInitializer.InitializeAsync(CancellationToken.None);
}

app.Run();

static string GetRateLimitClientAddress(HttpContext httpContext)
{
    string? forwardedFor = httpContext.Request.Headers["X-Forwarded-For"]
        .FirstOrDefault();

    if (!string.IsNullOrWhiteSpace(forwardedFor))
    {
        string forwardedClientAddress = forwardedFor.Split(',', StringSplitOptions.TrimEntries)[0];

        if (!string.IsNullOrWhiteSpace(forwardedClientAddress))
        {
            return forwardedClientAddress;
        }
    }

    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-client";
}

static string GetConnectionString(IConfiguration configuration)
{
    string? configuredConnectionString = configuration.GetConnectionString("DefaultConnection");

    if (!string.IsNullOrWhiteSpace(configuredConnectionString))
    {
        return configuredConnectionString;
    }

    string? databaseUrl = configuration["DATABASE_URL"];

    if (string.IsNullOrWhiteSpace(databaseUrl))
    {
        throw new InvalidOperationException("The DefaultConnection connection string or DATABASE_URL is required.");
    }

    Uri databaseUri = new(databaseUrl);

    if (!string.Equals(databaseUri.Scheme, "postgres", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(databaseUri.Scheme, "postgresql", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("DATABASE_URL must use the postgres or postgresql scheme.");
    }

    string[] credentials = databaseUri.UserInfo.Split(':', 2);

    if (credentials.Length != 2 || string.IsNullOrWhiteSpace(databaseUri.AbsolutePath.Trim('/')))
    {
        throw new InvalidOperationException("DATABASE_URL must include database credentials and a database name.");
    }

    var connectionStringBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = databaseUri.Host,
        Port = databaseUri.IsDefaultPort ? 5432 : databaseUri.Port,
        Database = Uri.UnescapeDataString(databaseUri.AbsolutePath.Trim('/')),
        Username = Uri.UnescapeDataString(credentials[0]),
        Password = Uri.UnescapeDataString(credentials[1])
    };

    return connectionStringBuilder.ConnectionString;
}

public partial class Program
{
}
