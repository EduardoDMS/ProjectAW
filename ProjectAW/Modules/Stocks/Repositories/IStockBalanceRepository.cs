using ProjectAW.Modules.Stocks.Entities;

namespace ProjectAW.Modules.Stocks.Repositories;

/// <summary>Acceso de lectura y persistencia para saldos por ubicación y almacén.</summary>
public interface IStockBalanceRepository
{
    Task<List<StockUbicacion>> ObtenerSaldosUbicacionAsync(
        int idUbicacion, int idProducto, CancellationToken ct = default);

    Task CrearSaldoUbicacionAsync(StockUbicacion saldo, CancellationToken ct = default);
    Task ActualizarSaldoUbicacionAsync(StockUbicacion saldo, CancellationToken ct = default);

    Task<List<StockUbicacion>> ObtenerSaldosUbicacionAlmacenAsync(
        int idAlmacen, int idProducto, CancellationToken ct = default);

    Task<List<StockAlmacen>> ObtenerSaldosAlmacenAsync(
        int idAlmacen, int idProducto, CancellationToken ct = default);

    Task CrearSaldoAlmacenAsync(StockAlmacen saldo, CancellationToken ct = default);
    Task ActualizarSaldoAlmacenAsync(StockAlmacen saldo, CancellationToken ct = default);
}
