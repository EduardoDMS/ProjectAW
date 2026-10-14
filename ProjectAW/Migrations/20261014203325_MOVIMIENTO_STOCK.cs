using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAW.Migrations
{
    /// <inheritdoc />
    public partial class MOVIMIENTO_STOCK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovimientoStock",
                columns: table => new
                {
                    IdMovimientoStock = table.Column<int>(type: "int", nullable: false),
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    IdAlmacen = table.Column<int>(type: "int", nullable: false),
                    IdTipoOperacion = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    FchMovimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdGuia = table.Column<int>(type: "int", nullable: false),
                    IdUsuarioOperador = table.Column<int>(type: "int", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CantidadDiferenciada = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientoStock", x => x.IdMovimientoStock);
                    table.ForeignKey(
                        name: "FK_MovimientoStock_Almacenes_IdAlmacen",
                        column: x => x.IdAlmacen,
                        principalTable: "Almacenes",
                        principalColumn: "IdAlmacen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientoStock_CabeceraGuias_IdGuia",
                        column: x => x.IdGuia,
                        principalTable: "CabeceraGuias",
                        principalColumn: "IdGuia",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientoStock_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalTable: "Productos",
                        principalColumn: "IdProducto",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientoStock_TipoOperacionGuias_IdMovimientoStock",
                        column: x => x.IdMovimientoStock,
                        principalTable: "TipoOperacionGuias",
                        principalColumn: "IdTipoOperacionGuia",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientoStock_Usuarios_IdUsuarioOperador",
                        column: x => x.IdUsuarioOperador,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAlmacen",
                columns: table => new
                {
                    IdStockAlmacen = table.Column<int>(type: "int", nullable: false),
                    IdAlmacen = table.Column<int>(type: "int", nullable: false),
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    CantidadActual = table.Column<int>(type: "int", nullable: false),
                    FechaUltimoRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAlmacen", x => x.IdStockAlmacen);
                    table.ForeignKey(
                        name: "FK_StockAlmacen_Almacenes_IdStockAlmacen",
                        column: x => x.IdStockAlmacen,
                        principalTable: "Almacenes",
                        principalColumn: "IdAlmacen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAlmacen_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalTable: "Productos",
                        principalColumn: "IdProducto",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoStock_IdAlmacen",
                table: "MovimientoStock",
                column: "IdAlmacen");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoStock_IdGuia",
                table: "MovimientoStock",
                column: "IdGuia");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoStock_IdProducto",
                table: "MovimientoStock",
                column: "IdProducto");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoStock_IdUsuarioOperador",
                table: "MovimientoStock",
                column: "IdUsuarioOperador");

            migrationBuilder.CreateIndex(
                name: "IX_StockAlmacen_IdProducto",
                table: "StockAlmacen",
                column: "IdProducto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientoStock");

            migrationBuilder.DropTable(
                name: "StockAlmacen");
        }
    }
}
