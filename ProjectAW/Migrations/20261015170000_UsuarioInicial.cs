using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectAW.Infrastructure.Data;

namespace ProjectAW.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261015170000_UsuarioInicial")]
public sealed class UsuarioInicial : Migration
{
    private const string PasswordHash = "AQAAAAIAAYagAAAAEFiZIaoQOQt+GUfallr3YHiZNj7gEzsnw0wEeu4JgdnzbyTxUmW56F3pjPZt8P9GBg==";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE IdRol = 1)
    BEGIN
        DECLARE @NombreRolInicial nvarchar(50) =
            CASE WHEN EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Administrador')
                 THEN N'Administrador inicial'
                 ELSE N'Administrador'
            END;

        SET IDENTITY_INSERT dbo.Roles ON;
        INSERT INTO dbo.Roles (IdRol, Nombre) VALUES (1, @NombreRolInicial);
        SET IDENTITY_INSERT dbo.Roles OFF;
    END;

    INSERT INTO dbo.Usuarios
        (Nombre, Apellido, Username, Password, FechaRegistro, FechaActualizada, Activo, IdRol)
    VALUES
        (N'Administrador', N'Sistema', N'admin', N'{PasswordHash}', GETDATE(), NULL, 1, 1);
END;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($@"
DELETE FROM dbo.Usuarios
WHERE Username = N'admin' AND Password = N'{PasswordHash}' AND IdRol = 1;");
    }
}
