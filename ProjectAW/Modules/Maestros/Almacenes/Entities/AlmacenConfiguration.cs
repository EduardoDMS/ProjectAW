using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace ProjectAW.Modules.Maestros.Almacenes.Entities
{
    public class AlmacenConfiguration:IEntityTypeConfiguration<Almacen>
    {
        public void Configure(EntityTypeBuilder<Almacen> builder)
        {
            builder.HasKey(a => a.IdAlmacen);
            
            builder.Property(a => a.Codigo)
                   .HasMaxLength(50)
                   .IsRequired();
            
            builder.Property(a => a.Descripcion)
                   .HasMaxLength(150)
                   .IsRequired();
            
            builder.Property(a => a.Activo)
                   .HasDefaultValue(true);
            
            builder.HasIndex(a => a.Codigo)
                   .IsUnique();

            builder.Property(a => a.FchRegistro)
                   .HasDefaultValueSql("GETDATE()")
                   .ValueGeneratedOnAdd();

            builder.Property(a => a.FchModificacion)
                   .ValueGeneratedOnUpdate();
        }
    
    }
}
