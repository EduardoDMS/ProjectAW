using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAW.Migrations
{
    /// <inheritdoc />
    public partial class TablaAlmacen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Almacen",
                table: "Almacen");

            migrationBuilder.RenameTable(
                name: "Almacen",
                newName: "Almacenes");

            migrationBuilder.RenameIndex(
                name: "IX_Almacen_Codigo",
                table: "Almacenes",
                newName: "IX_Almacenes_Codigo");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FchRegistro",
                table: "Almacenes",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Almacenes",
                table: "Almacenes",
                column: "IdAlmacen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Almacenes",
                table: "Almacenes");

            migrationBuilder.RenameTable(
                name: "Almacenes",
                newName: "Almacen");

            migrationBuilder.RenameIndex(
                name: "IX_Almacenes_Codigo",
                table: "Almacen",
                newName: "IX_Almacen_Codigo");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FchRegistro",
                table: "Almacen",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Almacen",
                table: "Almacen",
                column: "IdAlmacen");
        }
    }
}
