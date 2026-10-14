using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Stocks.Entities
{
    public class StockAlmacenConfiguration :IEntityTypeConfiguration<StockAlmacen>
    {
        public void Configure(EntityTypeBuilder<StockAlmacen> builder)
        {
            builder.HasKey(s => s.IdStockAlmacen);

            builder.Property(s => s.CantidadActual);

            builder.Property(s => s.FechaUltimoRegistro)
                   .HasDefaultValueSql("GETDATE()")
                   .ValueGeneratedOnAdd();

            builder.HasOne(s => s.Almacen)
                   .WithMany(a => a.StockAlmacen)
                   .HasForeignKey(s => s.IdStockAlmacen)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Producto)
                   .WithMany(p => p.StockAlmacen)
                   .HasForeignKey(s => s.IdProducto)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
