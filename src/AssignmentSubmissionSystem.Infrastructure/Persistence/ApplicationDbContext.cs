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

    public DbSet<Subject> Subjects => Set<Subject>();

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
    }
}
