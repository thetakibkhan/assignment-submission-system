using AssignmentSubmissionSystem.Domain.Accounts;
using AssignmentSubmissionSystem.Domain.Academics;
using AssignmentSubmissionSystem.Domain.Assignments;
using AssignmentSubmissionSystem.Domain.Submissions;
using AssignmentSubmissionSystem.Domain.Notifications;
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

    public DbSet<AccountAuditEvent> AccountAuditEvents => Set<AccountAuditEvent>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();

    public DbSet<Submission> Submissions => Set<Submission>();

    public DbSet<SubmissionRevision> SubmissionRevisions => Set<SubmissionRevision>();

    public DbSet<SubmissionReviewRevision> SubmissionReviewRevisions => Set<SubmissionReviewRevision>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

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

        builder.Entity<AccountAuditEvent>(entity =>
        {
            entity.Property(auditEvent => auditEvent.ChangeSummary)
                .HasMaxLength(500)
                .IsRequired();
            entity.Property(auditEvent => auditEvent.EventType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.HasIndex(auditEvent => new { auditEvent.TargetUserId, auditEvent.OccurredAt });
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(auditEvent => auditEvent.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(auditEvent => auditEvent.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Assignment>(entity =>
        {
            entity.Property(assignment => assignment.Title).HasMaxLength(200).IsRequired();
            entity.Property(assignment => assignment.Description).HasMaxLength(5000);
            entity.Property(assignment => assignment.MaximumMarks).HasPrecision(10, 2);
            entity.Property(assignment => assignment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(assignment => new { assignment.TeacherUserId, assignment.UpdatedAt });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(assignment => assignment.TeacherUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ClassCourse>().WithMany().HasForeignKey(assignment => assignment.ClassCourseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Subject>().WithMany().HasForeignKey(assignment => assignment.SubjectId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Submission>(entity =>
        {
            entity.Property(submission => submission.AttachmentContentType).HasMaxLength(100);
            entity.Property(submission => submission.AttachmentFileName).HasMaxLength(255);
            entity.Property(submission => submission.AttachmentStorageName).HasMaxLength(100);
            entity.Property(submission => submission.Feedback).HasMaxLength(5000);
            entity.Property(submission => submission.Marks).HasPrecision(10, 2);
            entity.Property(submission => submission.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(submission => submission.TextAnswer).HasMaxLength(10000);
            entity.HasIndex(submission => new { submission.AssignmentId, submission.StudentUserId })
                .IsUnique();
            entity.HasOne<Assignment>()
                .WithMany()
                .HasForeignKey(submission => submission.AssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(submission => submission.StudentUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SubmissionRevision>(entity =>
        {
            entity.Property(revision => revision.AttachmentContentType).HasMaxLength(100);
            entity.Property(revision => revision.AttachmentFileName).HasMaxLength(255);
            entity.Property(revision => revision.AttachmentStorageName).HasMaxLength(100);
            entity.Property(revision => revision.TextAnswer).HasMaxLength(10000);
            entity.HasIndex(revision => new { revision.SubmissionId, revision.RecordedAt });
            entity.HasOne<Submission>()
                .WithMany()
                .HasForeignKey(revision => revision.SubmissionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SubmissionReviewRevision>(entity =>
        {
            entity.Property(revision => revision.Feedback).HasMaxLength(5000);
            entity.Property(revision => revision.Marks).HasPrecision(10, 2);
            entity.Property(revision => revision.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(revision => new { revision.SubmissionId, revision.RecordedAt });
            entity.HasOne<Submission>().WithMany().HasForeignKey(revision => revision.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserNotification>(entity =>
        {
            entity.Property(notification => notification.Message).HasMaxLength(500).IsRequired();
            entity.Property(notification => notification.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.HasIndex(notification => new { notification.RecipientUserId, notification.CreatedAt });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(notification => notification.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Assignment>().WithMany().HasForeignKey(notification => notification.AssignmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Submission>().WithMany().HasForeignKey(notification => notification.SubmissionId).OnDelete(DeleteBehavior.Restrict);
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
