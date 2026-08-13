using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Migrations;
    /// <inheritdoc />
    public partial class AddSubmissionReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "Submissions",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Marks",
                table: "Submissions",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubmissionReviewRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Feedback = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Marks = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionReviewRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionReviewRevisions_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionReviewRevisions_SubmissionId_RecordedAt",
                table: "SubmissionReviewRevisions",
                columns: new[] { "SubmissionId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmissionReviewRevisions");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Marks",
                table: "Submissions");
        }
    }
