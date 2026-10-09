using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Maestros.Operaciones.Entities
{
    public class TipoOperacionGuiaConfiguration : IEntityTypeConfiguration<TipoOperacionGuia>
    {
        public void Configure(EntityTypeBuilder<TipoOperacionGuia> builder)
        {
            builder.ToTable("TipoOperacionGuias");

            builder.HasKey(x => x.IdTipoOperacionGuia);

            builder.Property(x => x.Codigo).IsRequired().HasMaxLength(10);
            builder.HasIndex(x => x.Codigo).IsUnique();

            builder.Property(x => x.Nombre).IsRequired().HasMaxLength(50);

            builder.HasData(
                new TipoOperacionGuia { IdTipoOperacionGuia = TipoOperacionGuiaIds.Entrada, Codigo = "ENT", Nombre = "Entrada" },
                new TipoOperacionGuia { IdTipoOperacionGuia = TipoOperacionGuiaIds.Salida, Codigo = "SAL", Nombre = "Salida" },
                new TipoOperacionGuia { IdTipoOperacionGuia = TipoOperacionGuiaIds.Traslado, Codigo = "TRA", Nombre = "Traslado" });
        }
    }
}
