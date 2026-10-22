using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Guias.Entities;

public sealed class TipoComportamientoGuiaConfiguration : IEntityTypeConfiguration<TipoComportamientoGuia>
{
    public void Configure(EntityTypeBuilder<TipoComportamientoGuia> builder)
    {
        builder.ToTable("TiposComportamientoGuia");
        builder.HasKey(x => x.IdTipoComportamiento);
        builder.Property(x => x.NombreComportamiento).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.NombreComportamiento).IsUnique();
        builder.Property(x => x.DescripcionComportamiento).HasMaxLength(250).IsRequired();
        builder.HasData(
            new TipoComportamientoGuia { IdTipoComportamiento = 1, NombreComportamiento = "MOVIMIENTO_NORMAL", DescripcionComportamiento = "La entrada suma existencias y la salida las resta." },
            new TipoComportamientoGuia { IdTipoComportamiento = 2, NombreComportamiento = "TRASLADO_INTERNO", DescripcionComportamiento = "La salida resta en origen y crea una entrada relacionada para sumar en destino." });
    }
}
