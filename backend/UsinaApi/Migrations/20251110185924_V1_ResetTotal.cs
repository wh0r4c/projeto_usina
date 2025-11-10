using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UsinaApi.Migrations
{
    /// <inheritdoc />
    public partial class V1_ResetTotal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Avisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Conteudo = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    TextoParaFala = table.Column<string>(type: "TEXT", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NivelPrioridade = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Faqs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Pergunta = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Resposta = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    TextoParaFala = table.Column<string>(type: "TEXT", nullable: false),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faqs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Cpf = table.Column<string>(type: "TEXT", nullable: false),
                    PinHash = table.Column<string>(type: "TEXT", nullable: true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Matricula = table.Column<string>(type: "TEXT", nullable: false),
                    PinFoiDefinido = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BancoHoras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    HorasAcumuladas = table.Column<decimal>(type: "decimal(10, 2)", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TextoParaFala = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BancoHoras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BancoHoras_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ferias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DataFim = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DiasDeSaldo = table.Column<int>(type: "INTEGER", nullable: false),
                    TextoParaFala = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ferias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ferias_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GravacoesRh",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    CaminhoArquivo = table.Column<string>(type: "TEXT", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Resolvido = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GravacoesRh", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GravacoesRh_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Holerites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    MesAno = table.Column<string>(type: "TEXT", nullable: false),
                    ValorLiquido = table.Column<decimal>(type: "TEXT", nullable: false),
                    TextoParaFala = table.Column<string>(type: "TEXT", nullable: false),
                    CaminhoPdf = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holerites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Holerites_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BancoHoras_UsuarioId",
                table: "BancoHoras",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Ferias_UsuarioId",
                table: "Ferias",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_GravacoesRh_UsuarioId",
                table: "GravacoesRh",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Holerites_UsuarioId",
                table: "Holerites",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Cpf",
                table: "Usuarios",
                column: "Cpf",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Avisos");

            migrationBuilder.DropTable(
                name: "BancoHoras");

            migrationBuilder.DropTable(
                name: "Faqs");

            migrationBuilder.DropTable(
                name: "Ferias");

            migrationBuilder.DropTable(
                name: "GravacoesRh");

            migrationBuilder.DropTable(
                name: "Holerites");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
