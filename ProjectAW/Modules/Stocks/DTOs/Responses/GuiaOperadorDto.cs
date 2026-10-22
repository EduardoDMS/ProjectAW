using ProjectAW.Modules.Guias.DTOs.Response;

namespace ProjectAW.Modules.Stocks.DTOs.Responses;

public sealed record GuiaOperadorDto(
    int IdGuia,
    string NumeroGuia,
    int IdTipoOperacionGuia,
    int? IdMotivoGuia,
    int? IdTipoComportamiento,
    string? NombreMotivo,
    bool RequiereProveedor,
    bool RequiereCliente,
    bool RequiereAlmacenOrigen,
    bool RequiereAlmacenDestino,
    string? TipoTercero,
    string? NombreTercero,
    string? NombreAlmacenOrigen,
    string? NombreAlmacenDestino,
    int? IdGuiaRelacionada,
    string? NumeroGuiaRelacionada,
    int IdEstadoGuia,
    int? IdProveedor,
    int? IdCliente,
    int? IdAlmacenOrigen,
    int? IdAlmacenDestino,
    DateTime FechaProgramada,
    DateTime FechaRegistro,
    DateTime? FechaActualizada,
    string? TipoDocumentoReferencia,
    string? NumeroDocumentoReferencia,
    string? Observacion,
    IReadOnlyList<DetalleGuiaResponse> Detalles);
