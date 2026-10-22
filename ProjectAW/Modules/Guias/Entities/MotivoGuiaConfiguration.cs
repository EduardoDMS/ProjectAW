using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAW.Modules.Maestros.Operaciones.Entities;

namespace ProjectAW.Modules.Guias.Entities;

public sealed class MotivoGuiaConfiguration : IEntityTypeConfiguration<MotivoGuia>
{
    public void Configure(EntityTypeBuilder<MotivoGuia> builder)
    {
        builder.ToTable("MotivosGuia", table => table.HasCheckConstraint(
            "CK_MotivosGuia_OperacionPermitida",
            $"IdTipoOperacionGuia IN ({TipoOperacionGuiaIds.Entrada}, {TipoOperacionGuiaIds.Salida})"));
        builder.HasKey(x => x.IdMotivoGuia);
        builder.Property(x => x.NombreMotivo).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.IdTipoOperacionGuia, x.NombreMotivo }).IsUnique();
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.HasOne(x => x.TipoOperacionGuia).WithMany().HasForeignKey(x => x.IdTipoOperacionGuia).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TipoComportamiento).WithMany(x => x.Motivos).HasForeignKey(x => x.IdTipoComportamiento).OnDelete(DeleteBehavior.Restrict);
        builder.HasData(
            new MotivoGuia { IdMotivoGuia = 1, NombreMotivo = "Recepción estándar", IdTipoOperacionGuia = 1, RequiereProveedor = true, RequiereAlmacenDestino = true, IdTipoComportamiento = 1, Activo = true },
            new MotivoGuia { IdMotivoGuia = 2, NombreMotivo = "Despacho a cliente", IdTipoOperacionGuia = 2, RequiereCliente = true, RequiereAlmacenOrigen = true, IdTipoComportamiento = 1, Activo = true },
            new MotivoGuia { IdMotivoGuia = 3, NombreMotivo = "Salida para traslado interno", IdTipoOperacionGuia = 2, RequiereAlmacenOrigen = true, RequiereAlmacenDestino = true, IdTipoComportamiento = 2, Activo = true },
            new MotivoGuia { IdMotivoGuia = 4, NombreMotivo = "Recepción de traslado interno", IdTipoOperacionGuia = 1, RequiereAlmacenDestino = true, IdTipoComportamiento = 1, Activo = true });
    }
}
