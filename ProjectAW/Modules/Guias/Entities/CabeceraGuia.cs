using ProjectAW.Modules.Maestros.Estados.Entities;

namespace ProjectAW.Modules.Guias.Entities
{ 
    public class CabeceraGuia
    {
        public int IdGuia { get; set; }
        public string NumeroGuia { get; set; } = string.Empty;
        
        public int IdTipoOperacionGuia { get; set; }
        public int IdEstadoGuia { get; set; } = EstadoGuiasIds.Pendiente;
        
        public int? IdProveedor { get; set; }
        public int? IdCliente { get; set; }
        public int? IdAlmacenOrigen { get; set; }
        public int? IdAlmacenDestino { get; set; }

        public DateTime FechaProgramada { get; set; }
       
        public string? TipoDocumentoReferencia { get; set; }
        public string? NumeroDocumentoReferencia { get; set; }
        public string? Observacion { get; set; }

        // fecha de hora de registro 
        public DateTime FechaRegistro { get; set; }

        public ICollection<DetalleGuia> Detalles { get; set; } = new List<DetalleGuia>();

    }
}
