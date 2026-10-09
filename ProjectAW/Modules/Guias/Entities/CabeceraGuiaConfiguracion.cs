using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAW.Modules.Maestros.Almacenes.Entities;
using ProjectAW.Modules.Maestros.Clientes.Entities;
using ProjectAW.Modules.Maestros.Proveedores.Entities;
using ProjectAW.Modules.Maestros.Estados.Entities;
using ProjectAW.Modules.Maestros.Operaciones.Entities;

namespace ProjectAW.Modules.Guias.Entities
{
    public class CabeceraGuiaConfiguracion : IEntityTypeConfiguration<CabeceraGuia>
    {
        public void Configure(EntityTypeBuilder<CabeceraGuia> builder)
        {
            builder.ToTable("CabeceraGuias", t =>
            {
                t.HasCheckConstraint("CK_CabeceraGuia_Operacion",
                    $@"(IdTipoOperacionGuia = {TipoOperacionGuiaIds.Entrada} AND IdProveedor IS NOT NULL AND IdAlmacenDestino IS NOT NULL
               AND IdCliente IS NULL AND IdAlmacenOrigen IS NULL)
          OR (IdTipoOperacionGuia = {TipoOperacionGuiaIds.Salida} AND IdAlmacenOrigen IS NOT NULL AND IdCliente IS NOT NULL
               AND IdProveedor IS NULL AND IdAlmacenDestino IS NULL)
          OR (IdTipoOperacionGuia = {TipoOperacionGuiaIds.Traslado} AND IdAlmacenOrigen IS NOT NULL AND IdAlmacenDestino IS NOT NULL
               AND IdAlmacenOrigen <> IdAlmacenDestino
               AND IdProveedor IS NULL AND IdCliente IS NULL)");
            });

            builder.HasKey(x => x.IdGuia);

            builder.Property(x => x.NumeroGuia)
                   .IsRequired()
                   .HasMaxLength(20);
            
            builder.HasIndex(x => x.NumeroGuia)
                   .IsUnique();

            builder.Property(x => x.FechaProgramada)
                   .HasColumnType("datetime2");
            
            builder.Property(x => x.TipoDocumentoReferencia)
                   .HasMaxLength(30);
            
            builder.Property(x => x.NumeroDocumentoReferencia)
                   .HasMaxLength(50);
            
            builder.Property(x => x.Observacion)
                   .HasMaxLength(500);

            builder.Property(x => x.FechaRegistro)
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("GETDATE()");

            builder.HasOne<Proveedor>().WithMany()
                   .HasForeignKey(x => x.IdProveedor)
                   .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne<Cliente>()
                   .WithMany()
                   .HasForeignKey(x => x.IdCliente)
                   .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne<Almacen>()
                   .WithMany()
                   .HasForeignKey(x => x.IdAlmacenOrigen)
                   .OnDelete(DeleteBehavior.Restrict);
           
            builder.HasOne<Almacen>()
                   .WithMany()
                   .HasForeignKey(x => x.IdAlmacenDestino)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Detalles)
                   .WithOne()
                   .HasForeignKey(d => d.IdGuia)
                   .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne<TipoOperacionGuia>()
                   .WithMany()
                   .HasForeignKey(x => x.IdTipoOperacionGuia)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<EstadoGuia>()
                   .WithMany()
                   .HasForeignKey(x => x.IdEstadoGuia)
                   .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
