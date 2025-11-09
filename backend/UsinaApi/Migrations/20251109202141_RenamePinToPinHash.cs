using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UsinaApi.Migrations
{
    /// <inheritdoc />
    public partial class RenamePinToPinHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Pin",
                table: "Usuarios",
                newName: "PinHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PinHash",
                table: "Usuarios",
                newName: "Pin");
        }
    }
}
