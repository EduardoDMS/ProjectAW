using ProjectAW.Modules.Guias.Entities;

namespace ProjectAW.Modules.Guias.DTOs.Response
{
    public record GuiaResponse(
     int IdGuia,
     string NumeroGuia,
     int IdTipoOperacionGuia,
     int IdEstadoGuia,
     int? IdProveedor,
     int? IdCliente,
     int? IdAlmacenOrigen,
     int? IdAlmacenDestino,
     DateTime FechaProgramada,
     DateTime FechaRegistro,
     string? TipoDocumentoReferencia,
     string? NumeroDocumentoReferencia,
     string? Observacion,
     IReadOnlyList<DetalleGuiaResponse> Detalles);
}
