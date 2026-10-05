using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ProjectAW.Modules.Maestros.Categorias.Entities
{
    public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Categoria> builder)
        {
            builder.HasKey(c => c.IdCategoria);
            builder.Property(c => c.Nombre)
                   .HasMaxLength(100)
                   .IsRequired();
            builder.Property(c => c.Descripcion)
                   .HasMaxLength(250);
            builder.Property(c => c.Activo)
                   .HasDefaultValue(true);
        }
    
    }
}
