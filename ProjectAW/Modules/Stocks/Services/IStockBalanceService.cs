namespace ProjectAW.Modules.Stocks.Services;

/// <summary>Aplica reglas comunes para mantener saldos de inventario consistentes.</summary>
public interface IStockBalanceService
{
    Task AjustarUbicacionAsync(
        int idUbicacion, int idProducto, int diferencia, CancellationToken ct = default);

    Task SincronizarAlmacenAsync(
        int idAlmacen, int idProducto, CancellationToken ct = default);
}
