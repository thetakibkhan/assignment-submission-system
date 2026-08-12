using System.Security.Claims;
using AssignmentSubmissionSystem.Api.Authentication;
using System.Text;
using AssignmentSubmissionSystem.Application.AccountManagement;
using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Application.Submissions;
using AssignmentSubmissionSystem.Application.AcademicSetup;
using AssignmentSubmissionSystem.Application.AcademicSetup.ClassCourses;
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("The DefaultConnection connection string is required.");

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.Configure<DemoAccountOptions>(builder.Configuration.GetSection(DemoAccountOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<IAccountManagementService, AccountManagementService>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IStudentAssignmentQuery, StudentAssignmentQuery>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<ISubmissionFileStorage, LocalSubmissionFileStorage>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IClassCourseRepository, ClassCourseRepository>();
builder.Services.AddScoped<IClassCourseService, ClassCourseService>();
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebApplication", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (IServiceScope scope = app.Services.CreateScope())
{
    DatabaseInitializer databaseInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await databaseInitializer.InitializeAsync(CancellationToken.None);
}

app.Run();

public partial class Program
{
}
