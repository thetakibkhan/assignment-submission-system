using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Migrations;
    /// <inheritdoc />
    public partial class AddMultipleSubmissionAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubmissionAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StorageName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionAttachments_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "SubmissionAttachments" ("Id", "SubmissionId", "FileName", "ContentType", "StorageName")
                SELECT "Id", "Id", "AttachmentFileName", "AttachmentContentType", "AttachmentStorageName"
                FROM "Submissions"
                WHERE "AttachmentFileName" IS NOT NULL
                  AND "AttachmentContentType" IS NOT NULL
                  AND "AttachmentStorageName" IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionAttachments_SubmissionId_Id",
                table: "SubmissionAttachments",
                columns: new[] { "SubmissionId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmissionAttachments");
        }
    }
