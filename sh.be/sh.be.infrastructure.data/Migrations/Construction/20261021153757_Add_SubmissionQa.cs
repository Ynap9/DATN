using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Construction
{
    /// <inheritdoc />
    public partial class Add_SubmissionQa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QaBy",
                schema: "construction",
                table: "Submission",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QaDate",
                schema: "construction",
                table: "Submission",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QaReason",
                schema: "construction",
                table: "Submission",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusQa",
                schema: "construction",
                table: "Submission",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SubmissionQaLog",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OldStatusQa = table.Column<int>(type: "int", nullable: false),
                    NewStatusQa = table.Column<int>(type: "int", nullable: false),
                    OldStatusCheck = table.Column<int>(type: "int", nullable: false),
                    NewStatusCheck = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionQaLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submission_ProjectId_DocumentTypeId_StatusQa",
                schema: "construction",
                table: "Submission",
                columns: new[] { "ProjectId", "DocumentTypeId", "StatusQa" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionQaLog_SubmissionId_CreatedDate",
                schema: "construction",
                table: "SubmissionQaLog",
                columns: new[] { "SubmissionId", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmissionQaLog",
                schema: "construction");

            migrationBuilder.DropIndex(
                name: "IX_Submission_ProjectId_DocumentTypeId_StatusQa",
                schema: "construction",
                table: "Submission");

            migrationBuilder.DropColumn(
                name: "QaBy",
                schema: "construction",
                table: "Submission");

            migrationBuilder.DropColumn(
                name: "QaDate",
                schema: "construction",
                table: "Submission");

            migrationBuilder.DropColumn(
                name: "QaReason",
                schema: "construction",
                table: "Submission");

            migrationBuilder.DropColumn(
                name: "StatusQa",
                schema: "construction",
                table: "Submission");
        }
    }
}
