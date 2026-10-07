using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Maestros.TipoDocumentos.Entities
{
    public class TipoDocumentoConfiguration:IEntityTypeConfiguration<TipoDocumento>
    {
        public void Configure(EntityTypeBuilder<TipoDocumento> builder)
        {
            builder.HasKey(t => t.IdTipoDocumento);

            builder.Property(t => t.Documento)
                    .HasMaxLength(10)
                    .IsRequired();

            builder.HasIndex(t => t.Documento)
                    .IsUnique();
        }
    }
}
