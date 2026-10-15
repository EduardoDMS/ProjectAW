using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Stocks.Entities;

public class StockUbicacionConfiguration : IEntityTypeConfiguration<StockUbicacion>
{
    public void Configure(EntityTypeBuilder<StockUbicacion> builder)
    {
        builder.HasKey(s => s.IdStockUbicacion);
        builder.HasIndex(s => new { s.IdProducto, s.IdUbicacion }).IsUnique();
        builder.ToTable("StockUbicacion", t => t.HasCheckConstraint("CK_StockUbicacion_Cantidad", "CantidadActual >= 0"));
        builder.Property(s => s.FechaUltimoRegistro).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        builder.HasOne(s => s.Producto).WithMany().HasForeignKey(s => s.IdProducto).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Ubicacion).WithMany().HasForeignKey(s => s.IdUbicacion).OnDelete(DeleteBehavior.Restrict);
    }
}
