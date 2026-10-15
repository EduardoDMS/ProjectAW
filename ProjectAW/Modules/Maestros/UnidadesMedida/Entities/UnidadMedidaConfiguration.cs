using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Maestros.UnidadesMedida.Entities;

public class UnidadMedidaConfiguration : IEntityTypeConfiguration<UnidadMedida>
{
    public void Configure(EntityTypeBuilder<UnidadMedida> b)
    {
        b.ToTable("UnidadesMedida");
        b.HasKey(x => x.IdUnidadMedida);
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Property(x => x.Codigo).HasMaxLength(10).IsRequired();
        b.HasIndex(x => x.Nombre).IsUnique();
        b.HasIndex(x => x.Codigo).IsUnique();
    }
}
