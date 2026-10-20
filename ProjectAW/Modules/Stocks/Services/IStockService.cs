using ProjectAW.Modules.Stocks.DTOs.Responses;

namespace ProjectAW.Modules.Stocks.Services;

public interface IStockService
{
    Task<IReadOnlyList<StockAlmacenDto>> ListarAsync(int? idAlmacen, int? idProducto, string? codigoUbicacion, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, long>> ObtenerDisponibilidadAsync(
        int idAlmacen, IReadOnlyCollection<int> idsProductos, CancellationToken ct = default);
}
