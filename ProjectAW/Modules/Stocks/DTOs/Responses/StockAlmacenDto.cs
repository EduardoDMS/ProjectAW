namespace ProjectAW.Modules.Stocks.DTOs.Responses;

public sealed record StockAlmacenDto(
    int IdStockUbicacion,
    int IdProducto,
    string CodigoProducto,
    string Producto,
    string Ubicacion,
    int CantidadActual,
    string UnidadMedida,
    DateTime FechaUltimoRegistro);
