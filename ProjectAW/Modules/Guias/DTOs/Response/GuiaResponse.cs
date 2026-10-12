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
     int IdUsuarioRegistro,
     int? IdUsuarioModificacion,
     DateTime? FechaActualizada,
     string? TipoDocumentoReferencia,
     string? NumeroDocumentoReferencia,
     string? Observacion,
     IReadOnlyList<DetalleGuiaResponse> Detalles);

    // De IdUsuarioRegistro hasta FechaActualizada es lo del api token :D 
}
