using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Maestros.Roles.Entities
{
    public class RolConfiguration:IEntityTypeConfiguration<Rol>
    {

        public void Configure(EntityTypeBuilder<Rol> builder)
        {
            builder.HasKey(r => r.IdRol);

            builder.Property(r => r.Nombre)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.HasIndex(r => r.Nombre)
                   .IsUnique();
        }
    }
}
