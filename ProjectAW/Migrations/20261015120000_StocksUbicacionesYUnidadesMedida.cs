using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectAW.Infrastructure.Data;

namespace ProjectAW.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261015120000_StocksUbicacionesYUnidadesMedida")]
public sealed class StocksUbicacionesYUnidadesMedida : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UnidadesMedida",
            columns: table => new
            {
                IdUnidadMedida = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Codigo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                Activo = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_UnidadesMedida", x => x.IdUnidadMedida));
        migrationBuilder.CreateIndex("IX_UnidadesMedida_Nombre", "UnidadesMedida", "Nombre", unique: true);
        migrationBuilder.CreateIndex("IX_UnidadesMedida_Codigo", "UnidadesMedida", "Codigo", unique: true);
        migrationBuilder.Sql("INSERT INTO [UnidadesMedida] ([Nombre],[Codigo],[Activo]) VALUES (N'Unidad',N'UND',1);");

        migrationBuilder.AddColumn<int>(name: "IdUnidadMedida", table: "Productos", type: "int", nullable: false, defaultValue: 1);
        migrationBuilder.CreateIndex("IX_Productos_IdUnidadMedida", "Productos", "IdUnidadMedida");
        migrationBuilder.AddForeignKey("FK_Productos_UnidadesMedida_IdUnidadMedida", "Productos", "IdUnidadMedida", "UnidadesMedida", principalColumn: "IdUnidadMedida", onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddColumn<int>(name: "IdUbicacionOrigen", table: "MovimientoStock", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "IdUbicacionDestino", table: "MovimientoStock", type: "int", nullable: true);
        migrationBuilder.CreateIndex("IX_MovimientoStock_IdUbicacionOrigen", "MovimientoStock", "IdUbicacionOrigen");
        migrationBuilder.CreateIndex("IX_MovimientoStock_IdUbicacionDestino", "MovimientoStock", "IdUbicacionDestino");
        migrationBuilder.AddForeignKey("FK_MovimientoStock_Ubicaciones_IdUbicacionOrigen", "MovimientoStock", "IdUbicacionOrigen", "Ubicaciones", principalColumn: "IdUbicacion", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("FK_MovimientoStock_Ubicaciones_IdUbicacionDestino", "MovimientoStock", "IdUbicacionDestino", "Ubicaciones", principalColumn: "IdUbicacion", onDelete: ReferentialAction.Restrict);

        migrationBuilder.CreateTable(
            name: "StockUbicacion",
            columns: table => new
            {
                IdStockUbicacion = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                IdProducto = table.Column<int>(type: "int", nullable: false),
                IdUbicacion = table.Column<int>(type: "int", nullable: false),
                CantidadActual = table.Column<int>(type: "int", nullable: false),
                FechaUltimoRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockUbicacion", x => x.IdStockUbicacion);
                table.CheckConstraint("CK_StockUbicacion_Cantidad", "CantidadActual >= 0");
                table.ForeignKey("FK_StockUbicacion_Productos_IdProducto", x => x.IdProducto, "Productos", "IdProducto", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_StockUbicacion_Ubicaciones_IdUbicacion", x => x.IdUbicacion, "Ubicaciones", "IdUbicacion", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_StockUbicacion_IdProducto_IdUbicacion", "StockUbicacion", new[] { "IdProducto", "IdUbicacion" }, unique: true);
        migrationBuilder.CreateIndex("IX_StockUbicacion_IdUbicacion", "StockUbicacion", "IdUbicacion");

        // Conserva los saldos existentes en una ubicación de apertura por almacén.
        // Su código MIG-{IdAlmacen} permite reubicar físicamente el stock con un traslado posterior.
        migrationBuilder.Sql(@"
INSERT INTO [Ubicaciones] ([Codigo], [Descripcion], [Activo], [IdAlmacen])
SELECT CONCAT(N'MIG-', sa.[IdAlmacen]), N'Ubicación de apertura: stock previo sin ubicación', 1, sa.[IdAlmacen]
FROM [StockAlmacen] sa
WHERE sa.[CantidadActual] > 0
  AND NOT EXISTS (SELECT 1 FROM [Ubicaciones] u WHERE u.[Codigo] = CONCAT(N'MIG-', sa.[IdAlmacen]));

INSERT INTO [StockUbicacion] ([IdProducto], [IdUbicacion], [CantidadActual], [FechaUltimoRegistro])
SELECT sa.[IdProducto], u.[IdUbicacion], sa.[CantidadActual], sa.[FechaUltimoRegistro]
FROM [StockAlmacen] sa
JOIN [Ubicaciones] u ON u.[Codigo] = CONCAT(N'MIG-', sa.[IdAlmacen]) AND u.[IdAlmacen] = sa.[IdAlmacen]
WHERE sa.[CantidadActual] > 0;");

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("StockUbicacion");
        migrationBuilder.DropForeignKey("FK_MovimientoStock_Ubicaciones_IdUbicacionOrigen", "MovimientoStock");
        migrationBuilder.DropForeignKey("FK_MovimientoStock_Ubicaciones_IdUbicacionDestino", "MovimientoStock");
        migrationBuilder.DropIndex("IX_MovimientoStock_IdUbicacionOrigen", "MovimientoStock");
        migrationBuilder.DropIndex("IX_MovimientoStock_IdUbicacionDestino", "MovimientoStock");
        migrationBuilder.DropColumn("IdUbicacionOrigen", "MovimientoStock");
        migrationBuilder.DropColumn("IdUbicacionDestino", "MovimientoStock");
        migrationBuilder.DropForeignKey("FK_Productos_UnidadesMedida_IdUnidadMedida", "Productos");
        migrationBuilder.DropIndex("IX_Productos_IdUnidadMedida", "Productos");
        migrationBuilder.DropColumn("IdUnidadMedida", "Productos");
        migrationBuilder.DropTable("UnidadesMedida");
    }
}
