using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PI3_App.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCamposInadimplencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiasAtraso",
                table: "Pagamentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsInadimplente",
                table: "Pagamentos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QuantidadeNotificacoes",
                table: "Pagamentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaNotificacaoEnviada",
                table: "Pagamentos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReceitasMensais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    ReceitaTotal = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ReceitaRecebida = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ReceitaPendente = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    QuantidadePagamentos = table.Column<int>(type: "int", nullable: false),
                    QuantidadePagamentosRecebidos = table.Column<int>(type: "int", nullable: false),
                    QuantidadePagamentosPendentes = table.Column<int>(type: "int", nullable: false),
                    DataCalculo = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceitasMensais", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceitasMensais");

            migrationBuilder.DropColumn(
                name: "DiasAtraso",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "IsInadimplente",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "QuantidadeNotificacoes",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "UltimaNotificacaoEnviada",
                table: "Pagamentos");
        }
    }
}
