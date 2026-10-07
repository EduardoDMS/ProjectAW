using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Maestros.Ubicaciones.Entities
{
    public class UbicacionConfiguration : IEntityTypeConfiguration<Ubicacion>
    {
        public void Configure(EntityTypeBuilder<Ubicacion> builder)
        {
            builder.HasKey(u => u.IdUbicacion);

            builder.Property(u => u.Codigo)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(u => u.Descripcion)
                   .HasMaxLength(150);

            builder.Property(u => u.Activo)
                   .IsRequired();

            builder.Property(u => u.FchRegistro)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.HasIndex(u => u.Codigo)
                   .IsUnique();

            builder.Property(u => u.FchModificacion)
                   .ValueGeneratedOnUpdate();

            builder.HasOne(u => u.Almacen)
                   .WithMany(a => a.Ubicaciones)
                   .HasForeignKey(u => u.IdAlmacen)
                   .OnDelete(DeleteBehavior.Restrict);






        }
    }
}

