using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectAW.Infrastructure.Data;

namespace ProjectAW.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261015220000_CatalogosInicialesYPasswordPruebas")]
public sealed class CatalogosInicialesYPasswordPruebas : Migration
{
    // ASP.NET Core PasswordHasher (V3) hash for the temporary password "123".
    private const string PasswordHash = "AQAAAAIAAYagAAAAEOorOlSu9mla1AoYCuWksGAVcTX+rI+rBltMQNApGs0LJy4TEvKLX1YwS7e8kLzGAw==";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
SET IDENTITY_INSERT dbo.TipoOperacionGuias ON;
INSERT INTO dbo.TipoOperacionGuias (IdTipoOperacionGuia, Codigo, Nombre)
SELECT v.Id, v.Codigo, v.Nombre
FROM (VALUES (1, N'ENT', N'Entrada'), (2, N'SAL', N'Salida'), (3, N'TRA', N'Traslado')) AS v(Id, Codigo, Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TipoOperacionGuias t WHERE t.IdTipoOperacionGuia = v.Id)
  AND NOT EXISTS (SELECT 1 FROM dbo.TipoOperacionGuias t WHERE t.Codigo = v.Codigo);
SET IDENTITY_INSERT dbo.TipoOperacionGuias OFF;

SET IDENTITY_INSERT dbo.EstadoGuias ON;
INSERT INTO dbo.EstadoGuias (IdEstadoGuia, Codigo, Nombre)
SELECT v.Id, v.Codigo, v.Nombre
FROM (VALUES (1, N'PEN', N'Pendiente'), (2, N'PRO', N'En proceso'),
             (3, N'ATE', N'Atendida'), (4, N'CAN', N'Cancelada')) AS v(Id, Codigo, Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.EstadoGuias e WHERE e.IdEstadoGuia = v.Id)
  AND NOT EXISTS (SELECT 1 FROM dbo.EstadoGuias e WHERE e.Codigo = v.Codigo);
SET IDENTITY_INSERT dbo.EstadoGuias OFF;

INSERT INTO dbo.TipoDocument (Documento)
SELECT v.Documento
FROM (VALUES (N'RUC'), (N'DNI'), (N'CE')) AS v(Documento)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TipoDocument t WHERE t.Documento = v.Documento);

UPDATE dbo.Usuarios
SET Password = N'" + PasswordHash + @"'
WHERE Username = N'admin' AND IdRol = 1;");
    }

    // Seed data and a temporary credential should remain intact if this migration is rolled back.
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
