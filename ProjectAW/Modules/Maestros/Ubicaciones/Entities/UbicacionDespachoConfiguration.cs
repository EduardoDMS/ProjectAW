using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Maestros.Ubicaciones.Entities;

public sealed class UbicacionDespachoConfiguration : IEntityTypeConfiguration<UbicacionDespacho>
{
    public void Configure(EntityTypeBuilder<UbicacionDespacho> builder)
    {
        builder.ToTable("UbicacionesDespacho");
        builder.HasKey(x => new { x.IdAlmacen, x.IdUbicacion });
        builder.HasOne(x => x.Almacen).WithMany(x => x.UbicacionesDespacho).HasForeignKey(x => x.IdAlmacen).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Ubicacion).WithMany(x => x.UbicacionesDespacho).HasForeignKey(x => x.IdUbicacion).OnDelete(DeleteBehavior.Cascade);
    }
}
