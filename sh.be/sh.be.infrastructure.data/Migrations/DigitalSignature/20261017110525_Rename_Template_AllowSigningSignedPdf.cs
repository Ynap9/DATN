using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sh.be.infrastructure.data.Migrations.DigitalSignature
{
    /// <inheritdoc />
    public partial class Rename_Template_AllowSigningSignedPdf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsOverlaySigning",
                schema: "digitalsignature",
                table: "Template",
                newName: "AllowSigningSignedPdf");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AllowSigningSignedPdf",
                schema: "digitalsignature",
                table: "Template",
                newName: "IsOverlaySigning");
        }
    }
}
