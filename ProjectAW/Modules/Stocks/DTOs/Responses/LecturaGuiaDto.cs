namespace ProjectAW.Modules.Stocks.DTOs.Responses;

public sealed record LecturaGuiaDto(
    long IdLecturaGuia, int IdGuia, int IdProducto, string CodigoProducto,
    string DescripcionProducto, string CodigoLeido, int Cantidad, string? Lote,
    string? Serie, string? Observacion, int? IdUbicacionOrigen, string? UbicacionOrigen,
    int? IdUbicacionDestino, string? UbicacionDestino, DateTime FechaRegistroUtc);
