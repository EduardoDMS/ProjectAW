using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Maestros.Productos.Entities;

public class ProductoConfiguration:IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.HasKey(p => p.IdProducto);

        builder.Property(p => p.Codigo)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Descripcion)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.Descripcion)
            .HasMaxLength(250);

        builder.Property(p => p.Activo)
            .HasDefaultValue(true);

        builder.HasIndex(p => p.Codigo)
            .IsUnique();
    }
}
