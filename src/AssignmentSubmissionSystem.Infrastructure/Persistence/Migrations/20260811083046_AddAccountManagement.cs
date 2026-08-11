using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddAccountManagement : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "MustChangePassword",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "AccountAuditEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                TargetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ChangeSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccountAuditEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_AccountAuditEvents_AspNetUsers_ActorUserId",
                    column: x => x.ActorUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_AccountAuditEvents_AspNetUsers_TargetUserId",
                    column: x => x.TargetUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AccountAuditEvents_ActorUserId",
            table: "AccountAuditEvents",
            column: "ActorUserId");

        migrationBuilder.CreateIndex(
            name: "IX_AccountAuditEvents_TargetUserId_OccurredAt",
            table: "AccountAuditEvents",
            columns: new[] { "TargetUserId", "OccurredAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AccountAuditEvents");

        migrationBuilder.DropColumn(
            name: "MustChangePassword",
            table: "AspNetUsers");
    }
}
