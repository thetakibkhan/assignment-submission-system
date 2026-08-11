using AssignmentSubmissionSystem.Domain.Academics;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClassCourse> ClassCourses => Set<ClassCourse>();

    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<TeacherResponsibility> TeacherResponsibilities => Set<TeacherResponsibility>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName)
                .HasMaxLength(200)
                .IsRequired();
        });

        builder.Entity<ClassCourse>(entity =>
        {
            entity.Property(classCourse => classCourse.Code)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(classCourse => classCourse.Name)
                .HasMaxLength(200)
                .IsRequired();
            entity.HasIndex(classCourse => classCourse.Code)
                .IsUnique();
        });

        builder.Entity<Subject>(entity =>
        {
            entity.Property(subject => subject.Code)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(subject => subject.Name)
                .HasMaxLength(200)
                .IsRequired();
            entity.HasIndex(subject => subject.Code)
                .IsUnique();
        });

        builder.Entity<StudentEnrollment>(entity =>
        {
            entity.Ignore(enrollment => enrollment.IsActive);
            entity.HasIndex(enrollment => new { enrollment.StudentUserId, enrollment.ClassCourseId })
                .HasFilter("\"EndedAt\" IS NULL")
                .IsUnique();
            entity.HasOne<ClassCourse>()
                .WithMany()
                .HasForeignKey(enrollment => enrollment.ClassCourseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(enrollment => enrollment.StudentUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(enrollment => enrollment.EnrolledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(enrollment => enrollment.EndedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TeacherResponsibility>(entity =>
        {
            entity.Ignore(responsibility => responsibility.IsActive);
            entity.HasIndex(responsibility => new { responsibility.ClassCourseId, responsibility.SubjectId })
                .HasFilter("\"RevokedAt\" IS NULL")
                .IsUnique();
            entity.HasOne<ClassCourse>()
                .WithMany()
                .HasForeignKey(responsibility => responsibility.ClassCourseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Subject>()
                .WithMany()
                .HasForeignKey(responsibility => responsibility.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(responsibility => responsibility.TeacherUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(responsibility => responsibility.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(responsibility => responsibility.RevokedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
