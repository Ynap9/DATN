using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Construction
{
    /// <inheritdoc />
    public partial class Add_Construction_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "construction",
                table: "DocumentType",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Field",
                schema: "construction",
                table: "DocumentField",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentType_Name",
                schema: "construction",
                table: "DocumentType",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentField_DocumentTypeId_Field",
                schema: "construction",
                table: "DocumentField",
                columns: new[] { "DocumentTypeId", "Field" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentField_DocumentTypeId_OrderTableView",
                schema: "construction",
                table: "DocumentField",
                columns: new[] { "DocumentTypeId", "OrderTableView" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentType_Name",
                schema: "construction",
                table: "DocumentType");

            migrationBuilder.DropIndex(
                name: "IX_DocumentField_DocumentTypeId_Field",
                schema: "construction",
                table: "DocumentField");

            migrationBuilder.DropIndex(
                name: "IX_DocumentField_DocumentTypeId_OrderTableView",
                schema: "construction",
                table: "DocumentField");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "construction",
                table: "DocumentType",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Field",
                schema: "construction",
                table: "DocumentField",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
