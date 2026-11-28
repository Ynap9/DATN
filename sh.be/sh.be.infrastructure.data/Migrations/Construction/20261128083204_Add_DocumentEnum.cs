using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Construction
{
    /// <inheritdoc />
    public partial class Add_DocumentEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnumId",
                schema: "construction",
                table: "DocumentField",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentEnum",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_DocumentEnum", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentEnum_DocumentType_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "construction",
                        principalTable: "DocumentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentField_EnumId",
                schema: "construction",
                table: "DocumentField",
                column: "EnumId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentEnum_DocumentTypeId_Name",
                schema: "construction",
                table: "DocumentEnum",
                columns: new[] { "DocumentTypeId", "Name" },
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentField_DocumentEnum_EnumId",
                schema: "construction",
                table: "DocumentField",
                column: "EnumId",
                principalSchema: "construction",
                principalTable: "DocumentEnum",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentField_DocumentEnum_EnumId",
                schema: "construction",
                table: "DocumentField");

            migrationBuilder.DropTable(
                name: "DocumentEnum",
                schema: "construction");

            migrationBuilder.DropIndex(
                name: "IX_DocumentField_EnumId",
                schema: "construction",
                table: "DocumentField");

            migrationBuilder.DropColumn(
                name: "EnumId",
                schema: "construction",
                table: "DocumentField");
        }
    }
}
