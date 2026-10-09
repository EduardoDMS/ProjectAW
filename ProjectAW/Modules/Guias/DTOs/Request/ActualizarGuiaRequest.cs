namespace ProjectAW.Modules.Guias.DTOs.Request
{
    public class ActualizarGuiaRequest
    {
        public int? IdProveedor { get; set; }
        public int? IdCliente { get; set; }
        public int? IdAlmacenOrigen { get; set; }
        public int? IdAlmacenDestino { get; set; }
        public DateTime FechaProgramada { get; set; }

        public string? TipODocumentoReferencia { get; set; }
        public string? NumeroDocumentoReferencia { get; set; }
        public string? Observacion { get; set; }

        public List<DetalleGuiaRequest> Detalles { get; set; } = new();

    }
}
