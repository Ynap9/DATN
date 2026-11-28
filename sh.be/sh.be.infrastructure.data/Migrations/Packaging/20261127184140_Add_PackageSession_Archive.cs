using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.Packaging
{
    /// <inheritdoc />
    public partial class Add_PackageSession_Archive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchiveCode",
                schema: "packaging",
                table: "PackageSession",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchiveName",
                schema: "packaging",
                table: "PackageSession",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchiveCode",
                schema: "packaging",
                table: "PackageSession");

            migrationBuilder.DropColumn(
                name: "ArchiveName",
                schema: "packaging",
                table: "PackageSession");
        }
    }
}
