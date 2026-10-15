using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAW.Migrations
{
    /// <inheritdoc />
    public partial class PruebasModuloStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientoStock_TipoOperacionGuias_IdMovimientoStock",
                table: "MovimientoStock");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAlmacen_Almacenes_IdStockAlmacen",
                table: "StockAlmacen");

            // SQL Server no permite ALTER COLUMN para activar IDENTITY.
            // Se reconstruyen ambas tablas copiando las claves existentes para conservar sus datos.
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1 FROM dbo.StockAlmacen
                    GROUP BY IdAlmacen, IdProducto HAVING COUNT(*) > 1
                )
                    THROW 51001, 'No se puede reparar Stocks: hay saldos duplicados para un producto y almacén.', 1;

                IF EXISTS (
                    SELECT 1 FROM dbo.MovimientoStock
                    GROUP BY IdGuia, IdProducto HAVING COUNT(*) > 1
                )
                    THROW 51002, 'No se puede reparar Stocks: hay más de un movimiento para el mismo producto y guía.', 1;

                IF EXISTS (
                    SELECT 1 FROM dbo.StockAlmacen s
                    LEFT JOIN dbo.Almacenes a ON a.IdAlmacen = s.IdAlmacen
                    LEFT JOIN dbo.Productos p ON p.IdProducto = s.IdProducto
                    WHERE a.IdAlmacen IS NULL OR p.IdProducto IS NULL OR s.CantidadActual < 0
                )
                    THROW 51003, 'No se puede reparar Stocks: hay saldos con referencias inválidas o cantidades negativas.', 1;

                IF EXISTS (
                    SELECT 1 FROM dbo.MovimientoStock m
                    LEFT JOIN dbo.Almacenes a ON a.IdAlmacen = m.IdAlmacen
                    LEFT JOIN dbo.CabeceraGuias g ON g.IdGuia = m.IdGuia
                    LEFT JOIN dbo.Productos p ON p.IdProducto = m.IdProducto
                    LEFT JOIN dbo.TipoOperacionGuias t ON t.IdTipoOperacionGuia = m.IdTipoOperacion
                    LEFT JOIN dbo.Usuarios u ON u.IdUsuario = m.IdUsuarioOperador
                    WHERE a.IdAlmacen IS NULL OR g.IdGuia IS NULL OR p.IdProducto IS NULL
                       OR t.IdTipoOperacionGuia IS NULL OR u.IdUsuario IS NULL OR m.Cantidad < 0
                )
                    THROW 51004, 'No se puede reparar Stocks: hay movimientos con referencias inválidas o cantidades negativas.', 1;

                CREATE TABLE dbo.MovimientoStock_Rebuild (
                    IdMovimientoStock int IDENTITY(1,1) NOT NULL,
                    IdProducto int NOT NULL,
                    IdAlmacen int NOT NULL,
                    IdTipoOperacion int NOT NULL,
                    Cantidad int NOT NULL,
                    FchMovimiento datetime2 NOT NULL,
                    IdGuia int NOT NULL,
                    IdUsuarioOperador int NOT NULL,
                    Observacion nvarchar(300) NOT NULL,
                    CantidadDiferenciada int NOT NULL,
                    CONSTRAINT PK_MovimientoStock_Rebuild PRIMARY KEY (IdMovimientoStock)
                );

                SET IDENTITY_INSERT dbo.MovimientoStock_Rebuild ON;
                INSERT INTO dbo.MovimientoStock_Rebuild
                    (IdMovimientoStock, IdProducto, IdAlmacen, IdTipoOperacion, Cantidad,
                     FchMovimiento, IdGuia, IdUsuarioOperador, Observacion, CantidadDiferenciada)
                SELECT IdMovimientoStock, IdProducto, IdAlmacen, IdTipoOperacion, Cantidad,
                       FchMovimiento, IdGuia, IdUsuarioOperador, Observacion, CantidadDiferenciada
                FROM dbo.MovimientoStock;
                SET IDENTITY_INSERT dbo.MovimientoStock_Rebuild OFF;

                CREATE TABLE dbo.StockAlmacen_Rebuild (
                    IdStockAlmacen int IDENTITY(1,1) NOT NULL,
                    IdAlmacen int NOT NULL,
                    IdProducto int NOT NULL,
                    CantidadActual int NOT NULL,
                    FechaUltimoRegistro datetime2 NOT NULL CONSTRAINT DF_StockAlmacen_Rebuild_Fecha DEFAULT (GETDATE()),
                    CONSTRAINT PK_StockAlmacen_Rebuild PRIMARY KEY (IdStockAlmacen)
                );

                SET IDENTITY_INSERT dbo.StockAlmacen_Rebuild ON;
                INSERT INTO dbo.StockAlmacen_Rebuild
                    (IdStockAlmacen, IdAlmacen, IdProducto, CantidadActual, FechaUltimoRegistro)
                SELECT IdStockAlmacen, IdAlmacen, IdProducto, CantidadActual, FechaUltimoRegistro
                FROM dbo.StockAlmacen;
                SET IDENTITY_INSERT dbo.StockAlmacen_Rebuild OFF;

                DROP TABLE dbo.MovimientoStock;
                DROP TABLE dbo.StockAlmacen;

                EXEC sys.sp_rename N'dbo.MovimientoStock_Rebuild', N'MovimientoStock';
                EXEC sys.sp_rename N'dbo.StockAlmacen_Rebuild', N'StockAlmacen';
                EXEC sys.sp_rename N'dbo.PK_MovimientoStock_Rebuild', N'PK_MovimientoStock', N'OBJECT';
                EXEC sys.sp_rename N'dbo.PK_StockAlmacen_Rebuild', N'PK_StockAlmacen', N'OBJECT';
                EXEC sys.sp_rename N'dbo.DF_StockAlmacen_Rebuild_Fecha', N'DF_StockAlmacen_FechaUltimoRegistro', N'OBJECT';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_StockAlmacen_IdAlmacen_IdProducto",
                table: "StockAlmacen",
                columns: new[] { "IdAlmacen", "IdProducto" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockAlmacen_CantidadActual",
                table: "StockAlmacen",
                sql: "CantidadActual >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoStock_IdGuia_IdProducto",
                table: "MovimientoStock",
                columns: new[] { "IdGuia", "IdProducto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoStock_IdAlmacen",
                table: "MovimientoStock",
                column: "IdAlmacen");

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

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoStock_IdTipoOperacion",
                table: "MovimientoStock",
                column: "IdTipoOperacion");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MovimientoStock_Cantidad",
                table: "MovimientoStock",
                sql: "Cantidad >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoStock_Almacenes_IdAlmacen",
                table: "MovimientoStock",
                column: "IdAlmacen",
                principalTable: "Almacenes",
                principalColumn: "IdAlmacen",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoStock_CabeceraGuias_IdGuia",
                table: "MovimientoStock",
                column: "IdGuia",
                principalTable: "CabeceraGuias",
                principalColumn: "IdGuia",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoStock_Productos_IdProducto",
                table: "MovimientoStock",
                column: "IdProducto",
                principalTable: "Productos",
                principalColumn: "IdProducto",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoStock_TipoOperacionGuias_IdTipoOperacion",
                table: "MovimientoStock",
                column: "IdTipoOperacion",
                principalTable: "TipoOperacionGuias",
                principalColumn: "IdTipoOperacionGuia",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoStock_Usuarios_IdUsuarioOperador",
                table: "MovimientoStock",
                column: "IdUsuarioOperador",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockAlmacen_Almacenes_IdAlmacen",
                table: "StockAlmacen",
                column: "IdAlmacen",
                principalTable: "Almacenes",
                principalColumn: "IdAlmacen",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "THROW 51010, 'Esta migración reconstruye claves IDENTITY conservando datos; no se admite revertirla automáticamente.', 1;");
        }
    }
}
