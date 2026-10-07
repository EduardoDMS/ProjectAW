using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAW.Migrations
{
    /// <inheritdoc />
    public partial class Proveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    IdProveedor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RazonSocial = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IdTipoDocumento = table.Column<int>(type: "int", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NombreContacto = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CorreoContacto = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TelefonoContacto = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FchRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    FchModificacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.IdProveedor);
                    table.ForeignKey(
                        name: "FK_Proveedores_TipoDocumento_IdTipoDocumento",
                        column: x => x.IdTipoDocumento,
                        principalTable: "TipoDocumento",
                        principalColumn: "IdTipoDocumento",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TipoDocumento_Documento",
                table: "TipoDocumento",
                column: "Documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_IdTipoDocumento",
                table: "Proveedores",
                column: "IdTipoDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_NumeroDocumento",
                table: "Proveedores",
                column: "NumeroDocumento",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropIndex(
                name: "IX_TipoDocumento_Documento",
                table: "TipoDocumento");
        }
    }
}
