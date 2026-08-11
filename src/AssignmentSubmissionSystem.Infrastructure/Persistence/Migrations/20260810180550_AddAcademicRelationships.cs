using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddAcademicRelationships : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StudentEnrollments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StudentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                ClassCourseId = table.Column<Guid>(type: "uuid", nullable: false),
                EnrolledByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                EnrolledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EndedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StudentEnrollments", x => x.Id);
                table.ForeignKey(
                    name: "FK_StudentEnrollments_AspNetUsers_EndedByUserId",
                    column: x => x.EndedByUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_StudentEnrollments_AspNetUsers_EnrolledByUserId",
                    column: x => x.EnrolledByUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_StudentEnrollments_AspNetUsers_StudentUserId",
                    column: x => x.StudentUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_StudentEnrollments_ClassCourses_ClassCourseId",
                    column: x => x.ClassCourseId,
                    principalTable: "ClassCourses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TeacherResponsibilities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TeacherUserId = table.Column<Guid>(type: "uuid", nullable: false),
                ClassCourseId = table.Column<Guid>(type: "uuid", nullable: false),
                SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TeacherResponsibilities", x => x.Id);
                table.ForeignKey(
                    name: "FK_TeacherResponsibilities_AspNetUsers_AssignedByUserId",
                    column: x => x.AssignedByUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TeacherResponsibilities_AspNetUsers_RevokedByUserId",
                    column: x => x.RevokedByUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TeacherResponsibilities_AspNetUsers_TeacherUserId",
                    column: x => x.TeacherUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TeacherResponsibilities_ClassCourses_ClassCourseId",
                    column: x => x.ClassCourseId,
                    principalTable: "ClassCourses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TeacherResponsibilities_Subjects_SubjectId",
                    column: x => x.SubjectId,
                    principalTable: "Subjects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_StudentEnrollments_ClassCourseId",
            table: "StudentEnrollments",
            column: "ClassCourseId");

        migrationBuilder.CreateIndex(
            name: "IX_StudentEnrollments_EndedByUserId",
            table: "StudentEnrollments",
            column: "EndedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_StudentEnrollments_EnrolledByUserId",
            table: "StudentEnrollments",
            column: "EnrolledByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_StudentEnrollments_StudentUserId_ClassCourseId",
            table: "StudentEnrollments",
            columns: new[] { "StudentUserId", "ClassCourseId" },
            unique: true,
            filter: "\"EndedAt\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_TeacherResponsibilities_AssignedByUserId",
            table: "TeacherResponsibilities",
            column: "AssignedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_TeacherResponsibilities_ClassCourseId_SubjectId",
            table: "TeacherResponsibilities",
            columns: new[] { "ClassCourseId", "SubjectId" },
            unique: true,
            filter: "\"RevokedAt\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_TeacherResponsibilities_RevokedByUserId",
            table: "TeacherResponsibilities",
            column: "RevokedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_TeacherResponsibilities_SubjectId",
            table: "TeacherResponsibilities",
            column: "SubjectId");

        migrationBuilder.CreateIndex(
            name: "IX_TeacherResponsibilities_TeacherUserId",
            table: "TeacherResponsibilities",
            column: "TeacherUserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "StudentEnrollments");

        migrationBuilder.DropTable(
            name: "TeacherResponsibilities");
    }
}
