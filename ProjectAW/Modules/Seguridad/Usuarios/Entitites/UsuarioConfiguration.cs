using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Seguridad.Usuarios.Entitites
{
    public class UsuarioConfiguration:IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.HasKey(u => u.IdUsuario);

            builder.Property(u => u.Nombre)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(u => u.Apellido)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(u => u.Username)
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(u => u.Password)
                    .HasMaxLength(200);

            builder.Property(u => u.Activo)
                   .HasDefaultValue(true);

            builder.Property(u => u.FechaRegistro)
                   .HasDefaultValueSql("GETDATE()")
                   .ValueGeneratedOnAdd();

            builder.Property(u => u.FechaActualizada)
                   .IsRequired(false);


            builder.HasOne(u => u.rol)
                   .WithMany(r => r.Usuario)
                   .HasForeignKey(u => u.IdRol)
                   .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
