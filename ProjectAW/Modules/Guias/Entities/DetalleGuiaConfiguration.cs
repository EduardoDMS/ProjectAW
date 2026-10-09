using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAW.Modules.Maestros.Productos.Entities;

namespace ProjectAW.Modules.Guias.Entities
{

    public class DetalleGuiaConfiguration : IEntityTypeConfiguration<DetalleGuia>
    {
        public void Configure(EntityTypeBuilder<DetalleGuia> builder)
        {
            builder.ToTable("DetalleGuias", t =>
                t.HasCheckConstraint("CK_DetalleGuia_Cantidad", "CantidadEsperada > 0"));

            builder.HasKey(x => x.IdGuiaDetalle);
            builder.Property(x => x.CantidadEsperada).HasPrecision(18, 4);

            builder.HasOne<Producto>().WithMany()
                .HasForeignKey(x => x.IdProducto).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
