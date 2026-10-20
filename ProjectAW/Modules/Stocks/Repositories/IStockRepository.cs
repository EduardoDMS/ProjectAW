using ProjectAW.Modules.Stocks.Entities;

namespace ProjectAW.Modules.Stocks.Repositories;

public interface IStockRepository
{
    Task<IReadOnlyList<StockUbicacion>> ListarAsync(int? idAlmacen = null, int? idProducto = null, string? codigoUbicacion = null, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, long>> ObtenerDisponibilidadAsync(
        int idAlmacen, IReadOnlyCollection<int> idsProductos, CancellationToken ct = default);
}
