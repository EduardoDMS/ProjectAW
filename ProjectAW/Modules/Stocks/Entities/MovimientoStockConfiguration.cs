using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Stocks.Entities
{
    public class MovimientoStockConfiguration : IEntityTypeConfiguration<MovimientoStock>
    {
        public void Configure(EntityTypeBuilder<MovimientoStock> builder)
        {
            builder.HasKey(m => m.IdMovimientoStock);

            builder.Property(m => m.Cantidad)
                   .IsRequired();

            builder.Property(m => m.FchMovimiento)
                   .ValueGeneratedOnAdd();

            builder.Property(m => m.Observacion)
                   .HasMaxLength(300);

            builder.Property(m => m.CantidadDiferenciada);



            builder.HasOne(m => m.Producto)
                   .WithMany(p => p.MovimientoStocks)
                   .HasForeignKey(m => m.IdProducto)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Almacen)
                   .WithMany(a => a.MovimientoStock)
                   .HasForeignKey(m => m.IdAlmacen)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.OperacionGuias)
                   .WithMany(o => o.MovimientoSTock)
                   .HasForeignKey(m => m.IdMovimientoStock)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Guia)
                   .WithMany(g => g.MovimientoStocks)
                   .HasForeignKey(m => m.IdGuia)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Usuario)
                   .WithMany(u => u.MovimientoStock)
                   .HasForeignKey(m => m.IdUsuarioOperador)
                   .OnDelete(DeleteBehavior.Restrict);
        
        }
    }
}