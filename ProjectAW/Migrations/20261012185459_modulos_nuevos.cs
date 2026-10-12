using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAW.Migrations
{
    /// <inheritdoc />
    public partial class modulos_nuevos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_TipoDocumento_IdTipoDocumento",
                table: "Clientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Proveedores_TipoDocumento_IdTipoDocumento",
                table: "Proveedores");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TipoDocumento",
                table: "TipoDocumento");

            migrationBuilder.RenameTable(
                name: "TipoDocumento",
                newName: "TipoDocument");

            migrationBuilder.RenameIndex(
                name: "IX_TipoDocumento_Documento",
                table: "TipoDocument",
                newName: "IX_TipoDocument_Documento");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TipoDocument",
                table: "TipoDocument",
                column: "IdTipoDocumento");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_TipoDocument_IdTipoDocumento",
                table: "Clientes",
                column: "IdTipoDocumento",
                principalTable: "TipoDocument",
                principalColumn: "IdTipoDocumento",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Proveedores_TipoDocument_IdTipoDocumento",
                table: "Proveedores",
                column: "IdTipoDocumento",
                principalTable: "TipoDocument",
                principalColumn: "IdTipoDocumento",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_TipoDocument_IdTipoDocumento",
                table: "Clientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Proveedores_TipoDocument_IdTipoDocumento",
                table: "Proveedores");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TipoDocument",
                table: "TipoDocument");

            migrationBuilder.RenameTable(
                name: "TipoDocument",
                newName: "TipoDocumento");

            migrationBuilder.RenameIndex(
                name: "IX_TipoDocument_Documento",
                table: "TipoDocumento",
                newName: "IX_TipoDocumento_Documento");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TipoDocumento",
                table: "TipoDocumento",
                column: "IdTipoDocumento");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_TipoDocumento_IdTipoDocumento",
                table: "Clientes",
                column: "IdTipoDocumento",
                principalTable: "TipoDocumento",
                principalColumn: "IdTipoDocumento",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Proveedores_TipoDocumento_IdTipoDocumento",
                table: "Proveedores",
                column: "IdTipoDocumento",
                principalTable: "TipoDocumento",
                principalColumn: "IdTipoDocumento",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
