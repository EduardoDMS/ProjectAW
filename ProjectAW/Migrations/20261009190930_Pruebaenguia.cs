using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAW.Migrations
{
    /// <inheritdoc />
    public partial class Pruebaenguia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CabeceraGuias",
                columns: table => new
                {
                    IdGuia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroGuia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipoOperacion = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    IdProveedor = table.Column<int>(type: "int", nullable: true),
                    IdCliente = table.Column<int>(type: "int", nullable: true),
                    IdAlmacenOrigen = table.Column<int>(type: "int", nullable: true),
                    IdAlmacenDestino = table.Column<int>(type: "int", nullable: true),
                    FechaProgramada = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoDocumentoReferencia = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NumeroDocumentoReferencia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CabeceraGuias", x => x.IdGuia);
                    table.CheckConstraint("CK_CabeceraGuia_Operacion", "(TipoOperacion = 1 AND IdProveedor IS NOT NULL AND IdAlmacenDestino IS NOT NULL\r\n                       AND IdCliente IS NULL AND IdAlmacenOrigen IS NULL)\r\n                  OR (TipoOperacion = 2 AND IdAlmacenOrigen IS NOT NULL AND IdCliente IS NOT NULL\r\n                       AND IdProveedor IS NULL AND IdAlmacenDestino IS NULL)\r\n                  OR (TipoOperacion = 3 AND IdAlmacenOrigen IS NOT NULL AND IdAlmacenDestino IS NOT NULL\r\n                       AND IdAlmacenOrigen <> IdAlmacenDestino\r\n                       AND IdProveedor IS NULL AND IdCliente IS NULL)");
                    table.ForeignKey(
                        name: "FK_CabeceraGuias_Almacenes_IdAlmacenDestino",
                        column: x => x.IdAlmacenDestino,
                        principalTable: "Almacenes",
                        principalColumn: "IdAlmacen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CabeceraGuias_Almacenes_IdAlmacenOrigen",
                        column: x => x.IdAlmacenOrigen,
                        principalTable: "Almacenes",
                        principalColumn: "IdAlmacen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CabeceraGuias_Clientes_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Clientes",
                        principalColumn: "IdCliente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CabeceraGuias_Proveedores_IdProveedor",
                        column: x => x.IdProveedor,
                        principalTable: "Proveedores",
                        principalColumn: "IdProveedor",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetalleGuias",
                columns: table => new
                {
                    IdGuiaDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdGuia = table.Column<int>(type: "int", nullable: false),
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    CantidadEsperada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleGuias", x => x.IdGuiaDetalle);
                    table.CheckConstraint("CK_DetalleGuia_Cantidad", "CantidadEsperada > 0");
                    table.ForeignKey(
                        name: "FK_DetalleGuias_CabeceraGuias_IdGuia",
                        column: x => x.IdGuia,
                        principalTable: "CabeceraGuias",
                        principalColumn: "IdGuia",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetalleGuias_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalTable: "Productos",
                        principalColumn: "IdProducto",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CabeceraGuias_IdAlmacenDestino",
                table: "CabeceraGuias",
                column: "IdAlmacenDestino");

            migrationBuilder.CreateIndex(
                name: "IX_CabeceraGuias_IdAlmacenOrigen",
                table: "CabeceraGuias",
                column: "IdAlmacenOrigen");

            migrationBuilder.CreateIndex(
                name: "IX_CabeceraGuias_IdCliente",
                table: "CabeceraGuias",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_CabeceraGuias_IdProveedor",
                table: "CabeceraGuias",
                column: "IdProveedor");

            migrationBuilder.CreateIndex(
                name: "IX_CabeceraGuias_NumeroGuia",
                table: "CabeceraGuias",
                column: "NumeroGuia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetalleGuias_IdGuia",
                table: "DetalleGuias",
                column: "IdGuia");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleGuias_IdProducto",
                table: "DetalleGuias",
                column: "IdProducto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetalleGuias");

            migrationBuilder.DropTable(
                name: "CabeceraGuias");
        }
    }
}
