using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectAW.Infrastructure.Data;

#nullable disable

namespace ProjectAW.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261022160000_UbicacionesDespachoRelacion")]
public partial class UbicacionesDespachoRelacion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UbicacionesDespacho",
            columns: table => new
            {
                IdAlmacen = table.Column<int>(type: "int", nullable: false),
                IdUbicacion = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UbicacionesDespacho", x => new { x.IdAlmacen, x.IdUbicacion });
                table.ForeignKey("FK_UbicacionesDespacho_Almacenes_IdAlmacen", x => x.IdAlmacen, "Almacenes", "IdAlmacen", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_UbicacionesDespacho_Ubicaciones_IdUbicacion", x => x.IdUbicacion, "Ubicaciones", "IdUbicacion", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_UbicacionesDespacho_IdUbicacion", "UbicacionesDespacho", "IdUbicacion");
        migrationBuilder.Sql("INSERT INTO UbicacionesDespacho (IdAlmacen, IdUbicacion) SELECT IdAlmacen, IdUbicacion FROM Ubicaciones WHERE EsUbicacionDespacho = 1;");
        migrationBuilder.DropColumn(name: "EsUbicacionDespacho", table: "Ubicaciones");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "EsUbicacionDespacho", table: "Ubicaciones", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.Sql("UPDATE u SET EsUbicacionDespacho = 1 FROM Ubicaciones u INNER JOIN UbicacionesDespacho d ON d.IdUbicacion = u.IdUbicacion AND d.IdAlmacen = u.IdAlmacen;");
        migrationBuilder.DropTable(name: "UbicacionesDespacho");
    }
}
