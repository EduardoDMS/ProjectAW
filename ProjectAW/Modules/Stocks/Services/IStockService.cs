using ProjectAW.Modules.Stocks.DTOs.Responses;

namespace ProjectAW.Modules.Stocks.Services;

public interface IStockService
{
    Task<IReadOnlyList<StockAlmacenDto>> ListarAsync(int? idAlmacen, int? idProducto, CancellationToken ct = default);
}
