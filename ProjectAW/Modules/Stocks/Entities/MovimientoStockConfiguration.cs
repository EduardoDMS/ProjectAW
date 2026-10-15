using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Stocks.Entities
{
    public class MovimientoStockConfiguration : IEntityTypeConfiguration<MovimientoStock>
    {
        public void Configure(EntityTypeBuilder<MovimientoStock> builder)
        {
            builder.HasKey(m => m.IdMovimientoStock);
            builder.Property(m => m.IdMovimientoStock).ValueGeneratedOnAdd();
            builder.HasIndex(m => new { m.IdGuia, m.IdProducto }).IsUnique();
            builder.ToTable("MovimientoStock", table =>
                table.HasCheckConstraint("CK_MovimientoStock_Cantidad", "Cantidad >= 0"));

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
                   .HasForeignKey(m => m.IdTipoOperacion)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Guia)
                   .WithMany(g => g.MovimientoStocks)
                   .HasForeignKey(m => m.IdGuia)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Usuario)
                   .WithMany(u => u.MovimientoStock)
                   .HasForeignKey(m => m.IdUsuarioOperador)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.UbicacionOrigen).WithMany()
                   .HasForeignKey(m => m.IdUbicacionOrigen).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.UbicacionDestino).WithMany()
                   .HasForeignKey(m => m.IdUbicacionDestino).OnDelete(DeleteBehavior.Restrict);
        
        }
    }
}
