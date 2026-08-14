using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Migrations;

public partial class RenameClassCoursesToAcademicClasses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Assignments_ClassCourses_ClassCourseId", table: "Assignments");
        migrationBuilder.DropForeignKey(name: "FK_StudentEnrollments_ClassCourses_ClassCourseId", table: "StudentEnrollments");
        migrationBuilder.DropForeignKey(name: "FK_TeacherResponsibilities_ClassCourses_ClassCourseId", table: "TeacherResponsibilities");

        migrationBuilder.RenameTable(name: "ClassCourses", newName: "AcademicClasses");
        migrationBuilder.RenameIndex(name: "IX_ClassCourses_Code", table: "AcademicClasses", newName: "IX_AcademicClasses_Code");
        migrationBuilder.Sql("ALTER TABLE \"AcademicClasses\" RENAME CONSTRAINT \"PK_ClassCourses\" TO \"PK_AcademicClasses\";");

        RenameClassReferences(migrationBuilder, "ClassCourseId", "AcademicClassId");

        migrationBuilder.AddForeignKey(name: "FK_Assignments_AcademicClasses_AcademicClassId", table: "Assignments", column: "AcademicClassId", principalTable: "AcademicClasses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_StudentEnrollments_AcademicClasses_AcademicClassId", table: "StudentEnrollments", column: "AcademicClassId", principalTable: "AcademicClasses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_TeacherResponsibilities_AcademicClasses_AcademicClassId", table: "TeacherResponsibilities", column: "AcademicClassId", principalTable: "AcademicClasses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Assignments_AcademicClasses_AcademicClassId", table: "Assignments");
        migrationBuilder.DropForeignKey(name: "FK_StudentEnrollments_AcademicClasses_AcademicClassId", table: "StudentEnrollments");
        migrationBuilder.DropForeignKey(name: "FK_TeacherResponsibilities_AcademicClasses_AcademicClassId", table: "TeacherResponsibilities");

        RenameClassReferences(migrationBuilder, "AcademicClassId", "ClassCourseId");

        migrationBuilder.RenameIndex(name: "IX_AcademicClasses_Code", table: "AcademicClasses", newName: "IX_ClassCourses_Code");
        migrationBuilder.Sql("ALTER TABLE \"AcademicClasses\" RENAME CONSTRAINT \"PK_AcademicClasses\" TO \"PK_ClassCourses\";");
        migrationBuilder.RenameTable(name: "AcademicClasses", newName: "ClassCourses");

        migrationBuilder.AddForeignKey(name: "FK_Assignments_ClassCourses_ClassCourseId", table: "Assignments", column: "ClassCourseId", principalTable: "ClassCourses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_StudentEnrollments_ClassCourses_ClassCourseId", table: "StudentEnrollments", column: "ClassCourseId", principalTable: "ClassCourses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_TeacherResponsibilities_ClassCourses_ClassCourseId", table: "TeacherResponsibilities", column: "ClassCourseId", principalTable: "ClassCourses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    private static void RenameClassReferences(MigrationBuilder migrationBuilder, string oldName, string newName)
    {
        migrationBuilder.RenameColumn(name: oldName, table: "Assignments", newName: newName);
        migrationBuilder.RenameIndex(name: "IX_Assignments_" + oldName, table: "Assignments", newName: "IX_Assignments_" + newName);

        migrationBuilder.RenameColumn(name: oldName, table: "StudentEnrollments", newName: newName);
        migrationBuilder.RenameIndex(name: "IX_StudentEnrollments_" + oldName, table: "StudentEnrollments", newName: "IX_StudentEnrollments_" + newName);
        migrationBuilder.RenameIndex(name: "IX_StudentEnrollments_StudentUserId_" + oldName, table: "StudentEnrollments", newName: "IX_StudentEnrollments_StudentUserId_" + newName);

        migrationBuilder.RenameColumn(name: oldName, table: "TeacherResponsibilities", newName: newName);
        migrationBuilder.RenameIndex(name: "IX_TeacherResponsibilities_" + oldName + "_SubjectId", table: "TeacherResponsibilities", newName: "IX_TeacherResponsibilities_" + newName + "_SubjectId");
    }
}
