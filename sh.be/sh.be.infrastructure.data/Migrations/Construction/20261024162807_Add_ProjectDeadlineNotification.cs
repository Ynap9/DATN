using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Construction
{
    /// <inheritdoc />
    public partial class Add_ProjectDeadlineNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectDeadlineNotification",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NotificationType = table.Column<int>(type: "int", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaysLeft = table.Column<int>(type: "int", nullable: false),
                    ToAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDeadlineNotification", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDeadlineNotification_CreatedDate",
                schema: "construction",
                table: "ProjectDeadlineNotification",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDeadlineNotification_Unique",
                schema: "construction",
                table: "ProjectDeadlineNotification",
                columns: new[] { "ProjectId", "NotificationType", "EndedAt", "ToAddress" },
                unique: true,
                filter: "[IsSuccess] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectDeadlineNotification",
                schema: "construction");
        }
    }
}
