using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PI3_App.Migrations
{
    /// <inheritdoc />
    public partial class AtualizarDocumentosHospede : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Documento",
                table: "Hospedes");

            migrationBuilder.AddColumn<string>(
                name: "CPF",
                table: "Hospedes",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EhBrasileiro",
                table: "Hospedes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumentoEstrangeiro",
                table: "Hospedes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RG",
                table: "Hospedes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumentoEstrangeiro",
                table: "Hospedes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CPF",
                table: "Hospedes");

            migrationBuilder.DropColumn(
                name: "EhBrasileiro",
                table: "Hospedes");

            migrationBuilder.DropColumn(
                name: "NumeroDocumentoEstrangeiro",
                table: "Hospedes");

            migrationBuilder.DropColumn(
                name: "RG",
                table: "Hospedes");

            migrationBuilder.DropColumn(
                name: "TipoDocumentoEstrangeiro",
                table: "Hospedes");

            migrationBuilder.AddColumn<string>(
                name: "Documento",
                table: "Hospedes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
