using ProjectAW.Modules.Stocks.Entities;

namespace ProjectAW.Modules.Stocks.Repositories;

public interface IStockRepository
{
    Task<IReadOnlyList<StockUbicacion>> ListarAsync(int? idAlmacen = null, int? idProducto = null, CancellationToken ct = default);
}
