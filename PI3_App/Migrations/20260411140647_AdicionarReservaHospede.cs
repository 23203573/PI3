using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PI3_App.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarReservaHospede : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceitasMensais");

            migrationBuilder.CreateTable(
                name: "ReservaHospedes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservaId = table.Column<int>(type: "int", nullable: false),
                    HospedeId = table.Column<int>(type: "int", nullable: false),
                    HospedePrincipal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservaHospedes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservaHospedes_Hospedes_HospedeId",
                        column: x => x.HospedeId,
                        principalTable: "Hospedes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservaHospedes_Reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "Reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservaHospedes_HospedeId",
                table: "ReservaHospedes",
                column: "HospedeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservaHospedes_ReservaId",
                table: "ReservaHospedes",
                column: "ReservaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservaHospedes");

            migrationBuilder.CreateTable(
                name: "ReceitasMensais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    DataCalculo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    QuantidadePagamentos = table.Column<int>(type: "int", nullable: false),
                    QuantidadePagamentosPendentes = table.Column<int>(type: "int", nullable: false),
                    QuantidadePagamentosRecebidos = table.Column<int>(type: "int", nullable: false),
                    ReceitaPendente = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ReceitaRecebida = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ReceitaTotal = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceitasMensais", x => x.Id);
                });
        }
    }
}
