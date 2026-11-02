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
            new MotivoGuia { IdMotivoGuia = 1, NombreMotivo = "Recepción estándar", IdTipoOperacionGuia = TipoOperacionGuiaIds.Entrada, RequiereProveedor = true, RequiereAlmacenDestino = true, IdTipoComportamiento = TipoComportamientoGuiaIds.MovimientoNormal, Activo = true },
            new MotivoGuia { IdMotivoGuia = 2, NombreMotivo = "Despacho a cliente", IdTipoOperacionGuia = TipoOperacionGuiaIds.Salida, RequiereCliente = true, RequiereAlmacenOrigen = true, IdTipoComportamiento = TipoComportamientoGuiaIds.MovimientoNormal, Activo = true },
            new MotivoGuia { IdMotivoGuia = 3, NombreMotivo = "Salida para traslado interno", IdTipoOperacionGuia = TipoOperacionGuiaIds.Salida, RequiereAlmacenOrigen = true, RequiereAlmacenDestino = true, IdTipoComportamiento = TipoComportamientoGuiaIds.TrasladoInterno, Activo = true },
            new MotivoGuia { IdMotivoGuia = 4, NombreMotivo = "Recepción de traslado interno", IdTipoOperacionGuia = TipoOperacionGuiaIds.Entrada, RequiereAlmacenDestino = true, IdTipoComportamiento = TipoComportamientoGuiaIds.MovimientoNormal, Activo = true });
    }
}
