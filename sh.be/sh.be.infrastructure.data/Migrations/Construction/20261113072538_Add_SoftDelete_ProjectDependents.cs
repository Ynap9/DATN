using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Construction
{
    /// <inheritdoc />
    public partial class Add_SoftDelete_ProjectDependents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmissionUser_SubmissionId_Role",
                schema: "construction",
                table: "SubmissionUser");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDeadlineNotification_Unique",
                schema: "construction",
                table: "ProjectDeadlineNotification");

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                schema: "construction",
                table: "SubmissionUser",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "construction",
                table: "SubmissionUser",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                schema: "construction",
                table: "SubmissionUser",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                schema: "construction",
                table: "ProjectDeadlineNotification",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "construction",
                table: "ProjectDeadlineNotification",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                schema: "construction",
                table: "ProjectDeadlineNotification",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                schema: "operation",
                table: "AssignmentTicketItem",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "operation",
                table: "AssignmentTicketItem",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                schema: "operation",
                table: "AssignmentTicketItem",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                schema: "operation",
                table: "AssignmentTicket",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "operation",
                table: "AssignmentTicket",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                schema: "operation",
                table: "AssignmentTicket",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionUser_SubmissionId_Role",
                schema: "construction",
                table: "SubmissionUser",
                columns: new[] { "SubmissionId", "Role" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDeadlineNotification_Unique",
                schema: "construction",
                table: "ProjectDeadlineNotification",
                columns: new[] { "ProjectId", "NotificationType", "EndedAt", "ToAddress" },
                unique: true,
                filter: "[IsSuccess] = 1 AND [Deleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmissionUser_SubmissionId_Role",
                schema: "construction",
                table: "SubmissionUser");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDeadlineNotification_Unique",
                schema: "construction",
                table: "ProjectDeadlineNotification");

            migrationBuilder.DropColumn(
                name: "Deleted",
                schema: "construction",
                table: "SubmissionUser");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "construction",
                table: "SubmissionUser");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                schema: "construction",
                table: "SubmissionUser");

            migrationBuilder.DropColumn(
                name: "Deleted",
                schema: "construction",
                table: "ProjectDeadlineNotification");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "construction",
                table: "ProjectDeadlineNotification");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                schema: "construction",
                table: "ProjectDeadlineNotification");

            migrationBuilder.DropColumn(
                name: "Deleted",
                schema: "operation",
                table: "AssignmentTicketItem");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "operation",
                table: "AssignmentTicketItem");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                schema: "operation",
                table: "AssignmentTicketItem");

            migrationBuilder.DropColumn(
                name: "Deleted",
                schema: "operation",
                table: "AssignmentTicket");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "operation",
                table: "AssignmentTicket");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                schema: "operation",
                table: "AssignmentTicket");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionUser_SubmissionId_Role",
                schema: "construction",
                table: "SubmissionUser",
                columns: new[] { "SubmissionId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDeadlineNotification_Unique",
                schema: "construction",
                table: "ProjectDeadlineNotification",
                columns: new[] { "ProjectId", "NotificationType", "EndedAt", "ToAddress" },
                unique: true,
                filter: "[IsSuccess] = 1");
        }
    }
}
