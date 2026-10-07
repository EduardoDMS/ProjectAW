using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAW.Migrations
{
    /// <inheritdoc />
    public partial class Relaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdAlmacen",
                table: "Ubicaciones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Ubicaciones_IdAlmacen",
                table: "Ubicaciones",
                column: "IdAlmacen");

            migrationBuilder.AddForeignKey(
                name: "FK_Ubicaciones_Almacenes_IdAlmacen",
                table: "Ubicaciones",
                column: "IdAlmacen",
                principalTable: "Almacenes",
                principalColumn: "IdAlmacen",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ubicaciones_Almacenes_IdAlmacen",
                table: "Ubicaciones");

            migrationBuilder.DropIndex(
                name: "IX_Ubicaciones_IdAlmacen",
                table: "Ubicaciones");

            migrationBuilder.DropColumn(
                name: "IdAlmacen",
                table: "Ubicaciones");
        }
    }
}
