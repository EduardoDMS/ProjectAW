using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectAW.Infrastructure.Data;

#nullable disable

namespace ProjectAW.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261019000000_LecturasIndividualesGuia")]
public partial class LecturasIndividualesGuia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LecturaGuia",
            columns: table => new
            {
                IdLecturaGuia = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                IdGuia = table.Column<int>(type: "int", nullable: false),
                IdProducto = table.Column<int>(type: "int", nullable: false),
                IdUsuarioOperador = table.Column<int>(type: "int", nullable: false),
                Cantidad = table.Column<int>(type: "int", nullable: false),
                CodigoLeido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Lote = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Serie = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Observacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                IdUbicacionOrigen = table.Column<int>(type: "int", nullable: true),
                IdUbicacionDestino = table.Column<int>(type: "int", nullable: true),
                FechaRegistroUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LecturaGuia", x => x.IdLecturaGuia);
                table.CheckConstraint("CK_LecturaGuia_Cantidad", "Cantidad >= 0");
                table.ForeignKey("FK_LecturaGuia_CabeceraGuias_IdGuia", x => x.IdGuia, "CabeceraGuias", "IdGuia", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_LecturaGuia_Productos_IdProducto", x => x.IdProducto, "Productos", "IdProducto", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_LecturaGuia_Usuarios_IdUsuarioOperador", x => x.IdUsuarioOperador, "Usuarios", "IdUsuario", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_LecturaGuia_Ubicaciones_IdUbicacionOrigen", x => x.IdUbicacionOrigen, "Ubicaciones", "IdUbicacion", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_LecturaGuia_Ubicaciones_IdUbicacionDestino", x => x.IdUbicacionDestino, "Ubicaciones", "IdUbicacion", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_LecturaGuia_IdGuia_FechaRegistroUtc", "LecturaGuia", new[] { "IdGuia", "FechaRegistroUtc" });
        migrationBuilder.CreateIndex("IX_LecturaGuia_IdProducto", "LecturaGuia", "IdProducto");
        migrationBuilder.CreateIndex("IX_LecturaGuia_IdUsuarioOperador", "LecturaGuia", "IdUsuarioOperador");
        migrationBuilder.CreateIndex("IX_LecturaGuia_IdUbicacionOrigen", "LecturaGuia", "IdUbicacionOrigen");
        migrationBuilder.CreateIndex("IX_LecturaGuia_IdUbicacionDestino", "LecturaGuia", "IdUbicacionDestino");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("LecturaGuia");
}
