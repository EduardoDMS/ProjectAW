namespace ProjectAW.Modules.Stocks.DTOs.Responses;

public sealed record StockAlmacenDto(
    int IdStockUbicacion,
    int IdProducto,
    string CodigoProducto,
    string Producto,
    int IdAlmacen,
    int IdUbicacion,
    string CodigoAlmacen,
    string Almacen,
    string Ubicacion,
    int CantidadActual,
    string UnidadMedida,
    DateTime FechaUltimoRegistro);
