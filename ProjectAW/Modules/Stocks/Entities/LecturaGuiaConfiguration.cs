using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Stocks.Entities;

public sealed class LecturaGuiaConfiguration : IEntityTypeConfiguration<LecturaGuia>
{
    public void Configure(EntityTypeBuilder<LecturaGuia> builder)
    {
        builder.ToTable("LecturaGuia", table => table.HasCheckConstraint("CK_LecturaGuia_Cantidad", "Cantidad >= 0"));
        builder.HasKey(x => x.IdLecturaGuia);
        builder.Property(x => x.CodigoLeido).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Lote).HasMaxLength(100);
        builder.Property(x => x.Serie).HasMaxLength(100);
        builder.Property(x => x.Observacion).HasMaxLength(300);
        builder.Property(x => x.FechaRegistroUtc).HasColumnType("datetime2");
        builder.HasIndex(x => new { x.IdGuia, x.FechaRegistroUtc });
        builder.HasOne(x => x.Guia).WithMany().HasForeignKey(x => x.IdGuia).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.IdProducto).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.UsuarioOperador).WithMany().HasForeignKey(x => x.IdUsuarioOperador).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.UbicacionOrigen).WithMany().HasForeignKey(x => x.IdUbicacionOrigen).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.UbicacionDestino).WithMany().HasForeignKey(x => x.IdUbicacionDestino).OnDelete(DeleteBehavior.Restrict);
    }
}
