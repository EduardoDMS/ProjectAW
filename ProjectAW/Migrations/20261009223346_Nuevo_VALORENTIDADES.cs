using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectAW.Migrations
{
    /// <inheritdoc />
    public partial class Nuevo_VALORENTIDADES : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CabeceraGuia_Operacion",
                table: "CabeceraGuias");

            migrationBuilder.RenameColumn(
                name: "TipoOperacion",
                table: "CabeceraGuias",
                newName: "IdTipoOperacionGuia");

            migrationBuilder.RenameColumn(
                name: "Estado",
                table: "CabeceraGuias",
                newName: "IdEstadoGuia");

            migrationBuilder.CreateTable(
                name: "EstadoGuias",
                columns: table => new
                {
                    IdEstadoGuia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoGuias", x => x.IdEstadoGuia);
                });

            migrationBuilder.CreateTable(
                name: "TipoOperacionGuias",
                columns: table => new
                {
                    IdTipoOperacionGuia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoOperacionGuias", x => x.IdTipoOperacionGuia);
                });

            migrationBuilder.InsertData(
                table: "EstadoGuias",
                columns: new[] { "IdEstadoGuia", "Codigo", "Nombre" },
                values: new object[,]
                {
                    { 1, "PEN", "Pendiente" },
                    { 2, "PRO", "En proceso" },
                    { 3, "ATE", "Atendida" },
                    { 4, "CAN", "Cancelada" }
                });

            migrationBuilder.InsertData(
                table: "TipoOperacionGuias",
                columns: new[] { "IdTipoOperacionGuia", "Codigo", "Nombre" },
                values: new object[,]
                {
                    { 1, "ENT", "Entrada" },
                    { 2, "SAL", "Salida" },
                    { 3, "TRA", "Traslado" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CabeceraGuias_IdEstadoGuia",
                table: "CabeceraGuias",
                column: "IdEstadoGuia");

            migrationBuilder.CreateIndex(
                name: "IX_CabeceraGuias_IdTipoOperacionGuia",
                table: "CabeceraGuias",
                column: "IdTipoOperacionGuia");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CabeceraGuia_Operacion",
                table: "CabeceraGuias",
                sql: "(IdTipoOperacionGuia = 1 AND IdProveedor IS NOT NULL AND IdAlmacenDestino IS NOT NULL\r\n               AND IdCliente IS NULL AND IdAlmacenOrigen IS NULL)\r\n          OR (IdTipoOperacionGuia = 2 AND IdAlmacenOrigen IS NOT NULL AND IdCliente IS NOT NULL\r\n               AND IdProveedor IS NULL AND IdAlmacenDestino IS NULL)\r\n          OR (IdTipoOperacionGuia = 3 AND IdAlmacenOrigen IS NOT NULL AND IdAlmacenDestino IS NOT NULL\r\n               AND IdAlmacenOrigen <> IdAlmacenDestino\r\n               AND IdProveedor IS NULL AND IdCliente IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_EstadoGuias_Codigo",
                table: "EstadoGuias",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TipoOperacionGuias_Codigo",
                table: "TipoOperacionGuias",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CabeceraGuias_EstadoGuias_IdEstadoGuia",
                table: "CabeceraGuias",
                column: "IdEstadoGuia",
                principalTable: "EstadoGuias",
                principalColumn: "IdEstadoGuia",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CabeceraGuias_TipoOperacionGuias_IdTipoOperacionGuia",
                table: "CabeceraGuias",
                column: "IdTipoOperacionGuia",
                principalTable: "TipoOperacionGuias",
                principalColumn: "IdTipoOperacionGuia",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CabeceraGuias_EstadoGuias_IdEstadoGuia",
                table: "CabeceraGuias");

            migrationBuilder.DropForeignKey(
                name: "FK_CabeceraGuias_TipoOperacionGuias_IdTipoOperacionGuia",
                table: "CabeceraGuias");

            migrationBuilder.DropTable(
                name: "EstadoGuias");

            migrationBuilder.DropTable(
                name: "TipoOperacionGuias");

            migrationBuilder.DropIndex(
                name: "IX_CabeceraGuias_IdEstadoGuia",
                table: "CabeceraGuias");

            migrationBuilder.DropIndex(
                name: "IX_CabeceraGuias_IdTipoOperacionGuia",
                table: "CabeceraGuias");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CabeceraGuia_Operacion",
                table: "CabeceraGuias");

            migrationBuilder.RenameColumn(
                name: "IdTipoOperacionGuia",
                table: "CabeceraGuias",
                newName: "TipoOperacion");

            migrationBuilder.RenameColumn(
                name: "IdEstadoGuia",
                table: "CabeceraGuias",
                newName: "Estado");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CabeceraGuia_Operacion",
                table: "CabeceraGuias",
                sql: "(TipoOperacion = 1 AND IdProveedor IS NOT NULL AND IdAlmacenDestino IS NOT NULL\r\n                       AND IdCliente IS NULL AND IdAlmacenOrigen IS NULL)\r\n                  OR (TipoOperacion = 2 AND IdAlmacenOrigen IS NOT NULL AND IdCliente IS NOT NULL\r\n                       AND IdProveedor IS NULL AND IdAlmacenDestino IS NULL)\r\n                  OR (TipoOperacion = 3 AND IdAlmacenOrigen IS NOT NULL AND IdAlmacenDestino IS NOT NULL\r\n                       AND IdAlmacenOrigen <> IdAlmacenDestino\r\n                       AND IdProveedor IS NULL AND IdCliente IS NULL)");
        }
    }
}
