using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Maestros.Clientes.Entities
{
    public class ClienteConfiguration:IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.HasKey(c => c.IdCliente);

            builder.Property(c => c.RazonSocial)
                    .HasMaxLength(50)
                    .IsRequired();

            builder.Property(c => c.IdTipoDocumento)
                    .IsRequired();

            builder.Property(c=>c.NumeroDocumento)
                    .HasMaxLength(25)
                    .IsRequired();

            builder.HasIndex(c => c.NumeroDocumento)
                    .IsUnique();

            builder.Property(c => c.Correo)
                    .HasMaxLength(50);

            builder.Property(c => c.Telefono)
                    .HasMaxLength(50);

            builder.Property(c=>c.Direccion)
                    .HasMaxLength (50);

            builder.Property(c => c.Activo)
                    .HasDefaultValue(true);

            builder.Property(c => c.FchRegistro)
                    .HasDefaultValueSql("GETDATE()")
                    .ValueGeneratedOnAdd();

            builder.Property(c => c.FchModificacion)
                    .IsRequired(false);

            builder.HasOne(c => c.TipoDocumento)
                    .WithMany(t => t.Clientes)
                    .HasForeignKey(c => c.IdTipoDocumento)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
