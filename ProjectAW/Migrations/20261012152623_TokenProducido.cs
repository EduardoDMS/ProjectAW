using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAW.Migrations
{
    /// <inheritdoc />
    public partial class TokenProducido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaActualizada",
                table: "CabeceraGuias",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdUsuarioModificacion",
                table: "CabeceraGuias",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdUsuarioRegistro",
                table: "CabeceraGuias",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CabeceraGuias_IdUsuarioModificacion",
                table: "CabeceraGuias",
                column: "IdUsuarioModificacion");

            migrationBuilder.CreateIndex(
                name: "IX_CabeceraGuias_IdUsuarioRegistro",
                table: "CabeceraGuias",
                column: "IdUsuarioRegistro");

            migrationBuilder.AddForeignKey(
                name: "FK_CabeceraGuias_Usuarios_IdUsuarioModificacion",
                table: "CabeceraGuias",
                column: "IdUsuarioModificacion",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CabeceraGuias_Usuarios_IdUsuarioRegistro",
                table: "CabeceraGuias",
                column: "IdUsuarioRegistro",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CabeceraGuias_Usuarios_IdUsuarioModificacion",
                table: "CabeceraGuias");

            migrationBuilder.DropForeignKey(
                name: "FK_CabeceraGuias_Usuarios_IdUsuarioRegistro",
                table: "CabeceraGuias");

            migrationBuilder.DropIndex(
                name: "IX_CabeceraGuias_IdUsuarioModificacion",
                table: "CabeceraGuias");

            migrationBuilder.DropIndex(
                name: "IX_CabeceraGuias_IdUsuarioRegistro",
                table: "CabeceraGuias");

            migrationBuilder.DropColumn(
                name: "FechaActualizada",
                table: "CabeceraGuias");

            migrationBuilder.DropColumn(
                name: "IdUsuarioModificacion",
                table: "CabeceraGuias");

            migrationBuilder.DropColumn(
                name: "IdUsuarioRegistro",
                table: "CabeceraGuias");
        }
    }
}
