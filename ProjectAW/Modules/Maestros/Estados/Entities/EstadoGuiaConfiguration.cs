using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectAW.Modules.Maestros.Estados.Entities
{
    public class EstadoGuiaConfiguration: IEntityTypeConfiguration<EstadoGuia>
    {
        public void Configure(EntityTypeBuilder<EstadoGuia> builder)
        {
            builder.ToTable("EstadoGuias");

            builder.HasKey(x => x.IdEstadoGuia);

            builder.Property(x => x.Codigo).IsRequired().HasMaxLength(20);
            builder.HasIndex(x => x.Codigo).IsUnique();

            builder.Property(x => x.Nombre).IsRequired().HasMaxLength(50);

            builder.HasData(
                new EstadoGuia { IdEstadoGuia = EstadoGuiasIds.Pendiente, Codigo = "PEN", Nombre = "Pendiente" },
                new EstadoGuia { IdEstadoGuia = EstadoGuiasIds.EnProceso, Codigo = "PRO", Nombre = "En proceso" },
                new EstadoGuia { IdEstadoGuia = EstadoGuiasIds.Atendida, Codigo = "ATE", Nombre = "Atendida" },
                new EstadoGuia { IdEstadoGuia = EstadoGuiasIds.Cancelada, Codigo = "CAN", Nombre = "Cancelada" });
        }

    }
}
