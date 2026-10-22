using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectAW.Migrations
{
    /// <inheritdoc />
    public partial class MotivosYComportamientoGuias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CabeceraGuia_Operacion",
                table: "CabeceraGuias");

            migrationBuilder.AddColumn<int>(
                name: "IdGuiaRelacionada",
                table: "CabeceraGuias",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdMotivoGuia",
                table: "CabeceraGuias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TiposComportamientoGuia",
                columns: table => new
                {
                    IdTipoComportamiento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreComportamiento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DescripcionComportamiento = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposComportamientoGuia", x => x.IdTipoComportamiento);
                });

            migrationBuilder.CreateTable(
                name: "MotivosGuia",
                columns: table => new
                {
                    IdMotivoGuia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreMotivo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdTipoOperacionGuia = table.Column<int>(type: "int", nullable: false),
                    RequiereProveedor = table.Column<bool>(type: "bit", nullable: false),
                    RequiereCliente = table.Column<bool>(type: "bit", nullable: false),
                    RequiereAlmacenOrigen = table.Column<bool>(type: "bit", nullable: false),
                    RequiereAlmacenDestino = table.Column<bool>(type: "bit", nullable: false),
                    IdTipoComportamiento = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotivosGuia", x => x.IdMotivoGuia);
                    table.CheckConstraint("CK_MotivosGuia_OperacionPermitida", "IdTipoOperacionGuia IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_MotivosGuia_TipoOperacionGuias_IdTipoOperacionGuia",
                        column: x => x.IdTipoOperacionGuia,
                        principalTable: "TipoOperacionGuias",
                        principalColumn: "IdTipoOperacionGuia",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MotivosGuia_TiposComportamientoGuia_IdTipoComportamiento",
                        column: x => x.IdTipoComportamiento,
                        principalTable: "TiposComportamientoGuia",
                        principalColumn: "IdTipoComportamiento",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "TiposComportamientoGuia",
                columns: new[] { "IdTipoComportamiento", "DescripcionComportamiento", "NombreComportamiento" },
                values: new object[,]
                {
                    { 1, "La entrada suma existencias y la salida las resta.", "MOVIMIENTO_NORMAL" },
                    { 2, "La salida resta en origen y crea una entrada relacionada para sumar en destino.", "TRASLADO_INTERNO" }
                });

            migrationBuilder.InsertData(
                table: "MotivosGuia",
                columns: new[] { "IdMotivoGuia", "Activo", "IdTipoComportamiento", "IdTipoOperacionGuia", "NombreMotivo", "RequiereAlmacenDestino", "RequiereAlmacenOrigen", "RequiereCliente", "RequiereProveedor" },
                values: new object[,]
                {
                    { 1, true, 1, 1, "Recepción estándar", true, false, false, true },
                    { 2, true, 1, 2, "Despacho a cliente", false, true, true, false },
                    { 3, true, 2, 2, "Salida para traslado interno", true, true, false, false },
                    { 4, true, 1, 1, "Recepción de traslado interno", true, false, false, false }
                });

            // Backfill existing entry/exit guides to the initial motives before adding the FK.
            migrationBuilder.Sql("UPDATE [CabeceraGuias] SET [IdMotivoGuia] = CASE [IdTipoOperacionGuia] WHEN 1 THEN 1 WHEN 2 THEN 2 END WHERE [IdTipoOperacionGuia] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_CabeceraGuias_IdGuiaRelacionada",
                table: "CabeceraGuias",
                column: "IdGuiaRelacionada");

            migrationBuilder.CreateIndex(
                name: "IX_CabeceraGuias_IdMotivoGuia",
                table: "CabeceraGuias",
                column: "IdMotivoGuia");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CabeceraGuia_Operacion",
                table: "CabeceraGuias",
                sql: "(IdTipoOperacionGuia IN (1, 2)\r\n               AND NOT (IdProveedor IS NOT NULL AND IdCliente IS NOT NULL)\r\n               AND (IdAlmacenOrigen IS NULL OR IdAlmacenDestino IS NULL OR IdAlmacenOrigen <> IdAlmacenDestino))\r\n          OR (IdTipoOperacionGuia = 3 AND IdAlmacenOrigen IS NOT NULL AND IdAlmacenDestino IS NOT NULL\r\n               AND IdAlmacenOrigen <> IdAlmacenDestino AND IdProveedor IS NULL AND IdCliente IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_MotivosGuia_IdTipoComportamiento",
                table: "MotivosGuia",
                column: "IdTipoComportamiento");

            migrationBuilder.CreateIndex(
                name: "IX_MotivosGuia_IdTipoOperacionGuia_NombreMotivo",
                table: "MotivosGuia",
                columns: new[] { "IdTipoOperacionGuia", "NombreMotivo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposComportamientoGuia_NombreComportamiento",
                table: "TiposComportamientoGuia",
                column: "NombreComportamiento",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CabeceraGuias_CabeceraGuias_IdGuiaRelacionada",
                table: "CabeceraGuias",
                column: "IdGuiaRelacionada",
                principalTable: "CabeceraGuias",
                principalColumn: "IdGuia",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CabeceraGuias_MotivosGuia_IdMotivoGuia",
                table: "CabeceraGuias",
                column: "IdMotivoGuia",
                principalTable: "MotivosGuia",
                principalColumn: "IdMotivoGuia",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CabeceraGuias_CabeceraGuias_IdGuiaRelacionada",
                table: "CabeceraGuias");

            migrationBuilder.DropForeignKey(
                name: "FK_CabeceraGuias_MotivosGuia_IdMotivoGuia",
                table: "CabeceraGuias");

            migrationBuilder.DropTable(
                name: "MotivosGuia");

            migrationBuilder.DropTable(
                name: "TiposComportamientoGuia");

            migrationBuilder.DropIndex(
                name: "IX_CabeceraGuias_IdGuiaRelacionada",
                table: "CabeceraGuias");

            migrationBuilder.DropIndex(
                name: "IX_CabeceraGuias_IdMotivoGuia",
                table: "CabeceraGuias");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CabeceraGuia_Operacion",
                table: "CabeceraGuias");

            migrationBuilder.DropColumn(
                name: "IdGuiaRelacionada",
                table: "CabeceraGuias");

            migrationBuilder.DropColumn(
                name: "IdMotivoGuia",
                table: "CabeceraGuias");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CabeceraGuia_Operacion",
                table: "CabeceraGuias",
                sql: "(IdTipoOperacionGuia = 1 AND IdProveedor IS NOT NULL AND IdAlmacenDestino IS NOT NULL\r\n               AND IdCliente IS NULL AND IdAlmacenOrigen IS NULL)\r\n          OR (IdTipoOperacionGuia = 2 AND IdAlmacenOrigen IS NOT NULL AND IdCliente IS NOT NULL\r\n               AND IdProveedor IS NULL AND IdAlmacenDestino IS NULL)\r\n          OR (IdTipoOperacionGuia = 3 AND IdAlmacenOrigen IS NOT NULL AND IdAlmacenDestino IS NOT NULL\r\n               AND IdAlmacenOrigen <> IdAlmacenDestino\r\n               AND IdProveedor IS NULL AND IdCliente IS NULL)");
        }
    }
}
