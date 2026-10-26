using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Construction
{
    /// <inheritdoc />
    public partial class Add_QaAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QaReviewerMode",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
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
                    table.PrimaryKey("PK_QaReviewerMode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QaReviewerMode_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "construction",
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QaSamplingConfig",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    SampleSize = table.Column<int>(type: "int", nullable: false),
                    ThresholdPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    StrategyCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StrategyParamsJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StopEarlyOnReject = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_QaSamplingConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QaSamplingConfig_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "construction",
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QaTicket",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    QaUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DocumentCount = table.Column<int>(type: "int", nullable: false),
                    SampleSize = table.Column<int>(type: "int", nullable: false),
                    MaxFailed = table.Column<int>(type: "int", nullable: false),
                    ThresholdPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    StrategyCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StrategyParamsJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Seed = table.Column<int>(type: "int", nullable: false),
                    StopEarlyOnReject = table.Column<bool>(type: "bit", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    RoundStartedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RejectedCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    ConcludedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_QaTicket", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QaTicket_DocumentType_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "construction",
                        principalTable: "DocumentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QaTicket_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "construction",
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionUser",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    IsSample = table.Column<bool>(type: "bit", nullable: false),
                    ReturnCount = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionUser_Submission_SubmissionId",
                        column: x => x.SubmissionId,
                        principalSchema: "construction",
                        principalTable: "Submission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QaReviewerMode_ProjectId_UserId",
                schema: "construction",
                table: "QaReviewerMode",
                columns: new[] { "ProjectId", "UserId" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_QaSamplingConfig_ProjectId",
                schema: "construction",
                table: "QaSamplingConfig",
                column: "ProjectId",
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_QaTicket_DocumentTypeId",
                schema: "construction",
                table: "QaTicket",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_QaTicket_ProjectId_DocumentTypeId_QaUserId",
                schema: "construction",
                table: "QaTicket",
                columns: new[] { "ProjectId", "DocumentTypeId", "QaUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_QaTicket_Status",
                schema: "construction",
                table: "QaTicket",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionUser_ProjectId_DocumentTypeId_Role_UserId",
                schema: "construction",
                table: "SubmissionUser",
                columns: new[] { "ProjectId", "DocumentTypeId", "Role", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionUser_SubmissionId_Role",
                schema: "construction",
                table: "SubmissionUser",
                columns: new[] { "SubmissionId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionUser_TicketId_Role",
                schema: "construction",
                table: "SubmissionUser",
                columns: new[] { "TicketId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionUser_UserId",
                schema: "construction",
                table: "SubmissionUser",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QaReviewerMode",
                schema: "construction");

            migrationBuilder.DropTable(
                name: "QaSamplingConfig",
                schema: "construction");

            migrationBuilder.DropTable(
                name: "QaTicket",
                schema: "construction");

            migrationBuilder.DropTable(
                name: "SubmissionUser",
                schema: "construction");
        }
    }
}
