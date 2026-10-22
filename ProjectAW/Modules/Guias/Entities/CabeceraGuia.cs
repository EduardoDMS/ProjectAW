using ProjectAW.Modules.Maestros.Estados.Entities;
using ProjectAW.Modules.Stocks.Entities;
using ProjectAW.Modules.Maestros.Almacenes.Entities;
using ProjectAW.Modules.Maestros.Clientes.Entities;
using ProjectAW.Modules.Maestros.Proveedores.Entities;

namespace ProjectAW.Modules.Guias.Entities
{ 
    public class CabeceraGuia
    {
        public int IdGuia { get; set; }
        public string NumeroGuia { get; set; } = string.Empty;
        
        public int IdTipoOperacionGuia { get; set; }
        public int? IdMotivoGuia { get; set; }
        public int? IdGuiaRelacionada { get; set; }
        public MotivoGuia? Motivo { get; set; }
        public CabeceraGuia? GuiaRelacionada { get; set; }
        public ICollection<CabeceraGuia> GuiasRelacionadas { get; set; } = new List<CabeceraGuia>();
        public int IdEstadoGuia { get; set; } = EstadoGuiasIds.Pendiente;
        
        public int? IdProveedor { get; set; }
        public int? IdCliente { get; set; }
        public int? IdAlmacenOrigen { get; set; }
        public int? IdAlmacenDestino { get; set; }
        public Proveedor? Proveedor { get; set; }
        public Cliente? Cliente { get; set; }
        public Almacen? AlmacenOrigen { get; set; }
        public Almacen? AlmacenDestino { get; set; }

        public DateTime FechaProgramada { get; set; }
       
        public string? TipoDocumentoReferencia { get; set; }
        public string? NumeroDocumentoReferencia { get; set; }
        public string? Observacion { get; set; }

        // fecha de hora de registro 
        public DateTime FechaRegistro { get; set; }

        public ICollection<DetalleGuia> Detalles { get; set; } = new List<DetalleGuia>();

        public int IdUsuarioRegistro { get; set; }
        public int? IdUsuarioModificacion { get; set; }

        public DateTime? FechaActualizada { get; set; }

        public ICollection<MovimientoStock>? MovimientoStocks { get; set; }


    }
}
