using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Operation
{
    /// <inheritdoc />
    public partial class Update_ProjectMember_Price : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectMemberPrice",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "ProjectPriceDefault",
                schema: "operation");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualPrice",
                schema: "operation",
                table: "ProjectMember",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CompanyPrice",
                schema: "operation",
                table: "ProjectMember",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualPrice",
                schema: "operation",
                table: "ProjectMember");

            migrationBuilder.DropColumn(
                name: "CompanyPrice",
                schema: "operation",
                table: "ProjectMember");

            migrationBuilder.CreateTable(
                name: "ProjectMemberPrice",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActualPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CompanyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                    ProjectMemberId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMemberPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMemberPrice_ProjectMember_ProjectMemberId",
                        column: x => x.ProjectMemberId,
                        principalSchema: "operation",
                        principalTable: "ProjectMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPriceDefault",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActualPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CompanyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPriceDefault", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPriceDefault_DocumentType_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "construction",
                        principalTable: "DocumentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectPriceDefault_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "construction",
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMemberPrice_ProjectMemberId_EffectiveFrom",
                schema: "operation",
                table: "ProjectMemberPrice",
                columns: new[] { "ProjectMemberId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPriceDefault_DocumentTypeId",
                schema: "operation",
                table: "ProjectPriceDefault",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPriceDefault_ProjectId_DocumentTypeId_Role_EffectiveFrom",
                schema: "operation",
                table: "ProjectPriceDefault",
                columns: new[] { "ProjectId", "DocumentTypeId", "Role", "EffectiveFrom" });
        }
    }
}
