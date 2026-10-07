using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Maestros.Proveedores.Entities
{
    public class ProveedorConfiguration:IEntityTypeConfiguration<Proveedor>
    {
        public void Configure(EntityTypeBuilder<Proveedor> builder)
        {
            builder.HasKey(p => p.IdProveedor);

            builder.Property(p => p.RazonSocial)
                    .HasMaxLength(50)
                    .IsRequired();

            builder.Property(p => p.IdTipoDocumento)
                    .IsRequired();

            builder.Property(p => p.Direccion)
                    .HasMaxLength(50);

            builder.Property(p=>p.NumeroDocumento)
                    .HasMaxLength(25)
                    .IsRequired();

            builder.HasIndex(p => p.NumeroDocumento)
                    .IsUnique();

            builder.Property(p => p.NombreContacto)
                    .HasMaxLength(50);

            builder.Property(p=>p.CorreoContacto)
                    .HasMaxLength (50);

            builder.Property(p => p.TelefonoContacto)
                    .HasMaxLength(50);

            builder.Property(p=>p.Activo)
                    .HasDefaultValue(true);

            builder.Property(p => p.FchRegistro)
                    .HasDefaultValueSql("GETDATE()")
                    .ValueGeneratedOnAdd();

            builder.Property(p => p.FchModificacion)
                    .IsRequired(false);

            builder.HasOne(p => p.TipoDocumento)
                    .WithMany(t => t.Proveedores)
                    .HasForeignKey(p => p.IdTipoDocumento)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
